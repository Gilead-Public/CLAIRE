using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Data;
using System.Data.DataSetExtensions;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft;
using MethodManager.Core;
using MMScriptObjects;
using MethodManager.Interop;
using MMScriptObjects.ScriptUtils;
/* 
** The script entry point will be the method Execute() in a unique instance of a class that implements IMMScriptExecutor.
*/
public class MMScriptExecutor : IMMScriptExecutor
{
	// This is the first method that gets executed.
	public void Execute(IMMApp app)
	{
		// Start adding new code here.
		
		//ResetVariables
		app.SetVariable("InputFile", "");
		
		app.SetVariable("SourceList", "");
		app.SetVariable("SourceLidList", "");
		app.SetVariable("DestList", "");
		app.SetVariable("DestLidList", "");
		app.SetVariable("MESourceList","");
		app.SetVariable("MEDestList","");
		
		app.SetVariable("VISeries", "");
		app.SetVariable("VIList", "");
		app.SetVariable("VMDISeries", "");
		app.SetVariable("VMDIList", "");
		app.SetVariable("BufferVISeries", "");
		app.SetVariable("BufferVIList", "");
		app.SetVariable("BufferVMDISeries", "");
		app.SetVariable("BufferVMDIList", "");
		app.SetVariable("CentrifugeVMDISeries","");
		app.SetVariable("CentrifugeVMDIList", "");
		app.SetVariable("CentrifugeVISeries","");
		app.SetVariable("CentrifugeVIList", "");
		
		app.SetVariable("SourceCodes", "");
		app.SetVariable("DestCodes", "");
		
		app.SetVariable("SourceType", "24Src");
		app.SetVariable("DestType", "24Dest");
		
		app.SetVariable("Centrifuge", "false");

		app.SetVariable("SourceLidOffset", "-33");
		app.SetVariable("DestLidOffset", "-33");
		
		string outputDir = "C:\\MethodManager4\\Workspaces\\LO5004\\Temp Worklists\\";
		string methodName = app.GetVariableValue("MethodName");
		int maxUniqueCol1 = 8;
		int maxUniqueCol3 = 8;
		int maxTips = 6;
		
		
		//User Dialog
		int worklistIndex = int.Parse(app.GetVariableValue("WorklistIndex"));
		if (worklistIndex == 0)
		{
		ClearDirectory(outputDir);
		MMScriptDialog Window1 = new MMScriptDialog();
		Window1.AddOpenFileDlg("Select Worklist", "", "csv", true, "InputFile");
		bool proceed = Window1.Show(app, "User Input", false, true);
		
		string inputFile = app.GetVariableValue("InputFile");	
		if (proceed)
		{
			int numWorklists = SplitWorklist(inputFile,outputDir,maxUniqueCol1,maxUniqueCol3);
			app.SetVariable("WorklistsInSet",numWorklists);
			app.SetVariable("WorklistIndex",1);
		}
		else
		{
			throw new Exception("Selection Aborted");
		}
		}
		
		string splitInputFile = outputDir + app.GetVariableValue("WorklistIndex") + ".csv";
		app.SetVariable("InputFile",splitInputFile);

			List<Worklist> worklist = new List<Worklist>();
			string filePath = app.GetVariableValue("InputFile");
			using (StreamReader sr = new StreamReader(filePath))
			{
		 		sr.ReadLine();
				while (sr.Peek() > -1)
				{
					string[] line = sr.ReadLine().Split(',');
					worklist.Add(new Worklist
					{
						SourceID = line[0],
						SourceWell = AddZero(line[1]),
						DestinationID = line[2],
						DestinationWell = AddZero(line[3]),
						Volume = line[4],
						BufferVolume = line[5],
						Centrifuge = line[6]

					});
				}
			}
		// Create VIs and VMDIs
		var groupedBySource = worklist.GroupBy(x => x.SourceID);
		int sourceCount = groupedBySource.Count();
		var groupedByDest = worklist.GroupBy(x => x.DestinationID);
		int destCount = groupedByDest.Count();
		var groupedByCent = worklist.GroupBy(x => x.Centrifuge);

		List<string> sourceIDs = new List<string>();
		List<string> sourceLidIDs = new List<string>();
		List<string> destIDs = new List<string>();
		List<string> destLidIDs = new List<string>();
		List<string> MESourceIDs = new List<string>();
		List<string> MEDestIDs = new List<string>();	
		
		bool hasCentrifuge = groupedByCent.Any(g => g.Key == "ME");
		
		if (hasCentrifuge)
			app.SetVariable("Centrifuge", "true");
		
		string[] aspVI = new string[97];
		string[] dispVMDI = new string[97];
		string[] buffVI = new string[97];
		string[] buffVMDI = new string [97];
		string[] centVI = new string[97];
		string[] centVMDI = new string [97];

		
		List<string> finalAspVI = new List<string>();
		List<string> finalDispVMDI = new List<string>();
		List<string> finalBuffVI = new List<string>();
		List<string> finalBuffVMDI = new List<string>();
		List<string> finalCentVI = new List<string>();
		List<string> finalCentVMDI = new List<string>();
		List<double> finalVolumes = new List<double>();

		//Pipetting Settings
		var map = Create24Map();
		var samplingMap = MakeMap();
		int index = 0;
		double dispVol = 0, transferVol, finalVol;
		int maxVolume = 1000;
		double remainder = 500;

		//Transfer VI and VMDI Generation
		foreach (var source in groupedBySource)
		{
			finalAspVI.Add("&");
			finalDispVMDI.Add("&");

			sourceIDs.Add("," + source.Key);
			sourceLidIDs.Add("," + source.Key + ".Lid");
			// Find largest volume
			var largestVolume = worklist.Max(x => double.Parse(x.Volume));
			var maxTransferNum = Math.Ceiling(largestVolume / maxVolume);

			for (int i = 0; i < maxTransferNum; i++)
			{
				aspVI = new string[97];
				dispVMDI = new string[97];
				
				aspVI[0] = "+VI;12;8";
				dispVMDI[0] = "+VMDI;12;8";

				foreach (var well in source)
				{
					if (well.Centrifuge != "P")
						continue;
					
					index = map[well.SourceWell];
					double totalVol = double.Parse(well.Volume);
					finalVol = (double.Parse(well.Volume)+double.Parse(well.BufferVolume));
					finalVolumes.Add(finalVol);
					int transferNum = (int)Math.Ceiling(totalVol / maxVolume);
					
					if (i < (maxTransferNum - transferNum))
						continue;
					
					transferVol = Math.Round(totalVol / transferNum);
					
					dispVol = transferVol * 1.1;
					
					aspVI[index] = transferVol.ToString();
					dispVMDI[index] = well.DestinationID + ":" + well.DestinationWell + ";" + dispVol.ToString() + ";;200";
				}

				finalAspVI.Add(string.Join(",", aspVI));
				finalDispVMDI.Add(string.Join(",", dispVMDI));
			}
		}
		
		//Buffer VI and VMDI Generation
		foreach (var source in groupedBySource)
		{
			finalBuffVI.Add("&");
			finalBuffVMDI.Add("&");
			
			// Find largest volume
			var largestVolume = worklist.Max(x => double.Parse(x.BufferVolume));
			var maxTransferNum = Math.Ceiling(largestVolume / maxVolume);

			for (int i = 0; i < maxTransferNum; i++)
			{
				buffVI = new string[97];
				buffVMDI = new string[97];
				
				buffVI[0] = "+VI;12;8";
				buffVMDI[0] = "+VMDI;12;8";

				foreach (var well in source)
				{
					if (well.Centrifuge != "P")
						continue;
					
					index = map[well.DestinationWell];
					double totalVol = double.Parse(well.BufferVolume);
					int transferNum = (int)Math.Ceiling(totalVol / maxVolume);
					
					if (i < (maxTransferNum - transferNum))
						continue;
					
					transferVol = Math.Round(totalVol / transferNum);
					
					dispVol = transferVol * 1.3;
				
					buffVI[index] = transferVol.ToString() + ";;20";
					buffVMDI[index] = well.DestinationID + ":" + well.DestinationWell + ";" + dispVol.ToString() + ";;100";
					
				}

				finalBuffVI.Add(string.Join(",", buffVI));
				finalBuffVMDI.Add(string.Join(",", buffVMDI));
			}
		}
		
		//Centrifuge VI and VMDI Generation
		foreach (var source in groupedBySource)
		{

			finalCentVI.Add("&");
			finalCentVMDI.Add("&");
			
			//Find Transfer Volume
			double CentVolume = finalVolumes.Max();
			var CentTransferNum = Math.Ceiling(CentVolume / maxVolume);
			double CentTransferVolume = Math.Round(CentVolume/CentTransferNum);
			
			for (int i = 0; i < CentTransferNum; i++)
			{ 
				centVI = new string[97];
				centVMDI = new string[97];
			
				centVI[0] = "+VI;12;8";
				centVMDI[0] = "+VMDI;12;8";

				foreach (var well in source)
				{
					if (well.Centrifuge != "ME")
						continue;
				
					MESourceIDs.Add("," + well.SourceID);
					MEDestIDs.Add("," + well.DestinationID);
					//int volume = 1000;
					index = map[well.DestinationWell];
					transferVol = CentTransferVolume;
					
					dispVol = transferVol * 1.1;
								
					centVI[index] = transferVol.ToString() + ";;20";
					centVMDI[index] = well.DestinationID + ":" + well.DestinationWell + ";" + dispVol.ToString() + ";;100";
				}
			finalCentVI.Add(string.Join(",", centVI));
			finalCentVMDI.Add(string.Join(",", centVMDI));
			}
		}
		// Media Exchange Supernatant Removal Volume Calculation
		
		double supernatantVol = (finalVolumes.Max()-remainder);
		var supernatantLoops = Math.Ceiling(supernatantVol / maxVolume);
		app.SetVariable("SupernatantLoop",((int)supernatantLoops));
		app.SetVariable("Transfervolume",(supernatantVol/supernatantLoops));
		
		
		// Destination ID List
		foreach (var dest in groupedByDest)
		{
			destIDs.Add("," + dest.Key);
			destLidIDs.Add("," + dest.Key + ".Lid");
		}
		
		//Media Exchange source and dest ID list
		MESourceIDs = MESourceIDs.Distinct().ToList();
		MEDestIDs = MEDestIDs.Distinct().ToList();
		
		
		// Set variables
		app.SetVariable("VISeries", string.Join("", finalAspVI));
		app.SetVariable("VMDISeries", string.Join("", finalDispVMDI));
		app.SetVariable("BufferVISeries", string.Join("", finalBuffVI));
		app.SetVariable("BufferVMDISeries", string.Join("", finalBuffVMDI));
		app.SetVariable("CentrifugeVISeries",string.Join("",finalCentVI));
		app.SetVariable("CentrifugeVMDISeries", string.Join("", finalCentVMDI));
		app.SetVariable("SourceList", string.Join("", sourceIDs));
		app.SetVariable("SourceLidList", string.Join("", sourceLidIDs));
		app.SetVariable("DestList", string.Join("", destIDs));
		app.SetVariable("DestLidList", string.Join("", destLidIDs));
		app.SetVariable("MESourceList", string.Join("", MESourceIDs));
		app.SetVariable("MEDestList", string.Join("", MEDestIDs));
		app.SetVariable("SourceCount",sourceCount);
		app.SetVariable("DestCount",destCount);
		string message = ("This is worklist " + app.GetVariableValue("WorklistIndex") + " of " + app.GetVariableValue("WorklistsInSet") + " in this set. Source Barcodes in this set are \r\n" + app.GetVariableValue("SourceList") + "\r\n and Destination Barcodes in this set are \r\n" + app.GetVariableValue("DestList") + ".");
		MMScriptDialog Window2 = new MMScriptDialog();
		Window2.AddMessage(message);
		bool proceed2 = Window2.Show(app, "Worklist split", false, true);
		
		if  (proceed2)
		{
		}
		else
		{
			throw new Exception("Selection Aborted");
		}
		
		//Remove unused plates from worktable
		string sourceName = app.GetVariableValue("SourceType") + "_";
		string destName = app.GetVariableValue("DestType") + "_";
		double tipCount = sourceCount + (2 * MESourceIDs.Count());	
		int tipBoxes = (int)Math.Ceiling(tipCount/4)+1;
		sourceCount+=1;
		destCount+=1;
		string tipsName = "TransferTips_";
		while (tipBoxes<=maxTips)
		{
			//MessageBox.Show(tipCount.ToString());
			app.RemovePlateFromWorktable("Lynx", "Left",(tipsName+tipBoxes.ToString("D2")));
			tipBoxes++;
		}
		while (sourceCount<=maxUniqueCol1)
		{
			app.RemovePlateFromWorktable("Lynx", "Left",(sourceName+sourceCount.ToString("D2")));
			app.RemovePlateFromWorktable("Lynx", "Right",(sourceName+sourceCount.ToString("D2")));
			sourceCount++;	
		}
		
		while (destCount<=maxUniqueCol3)
		{
			app.RemovePlateFromWorktable("Lynx", "Left", (destName+destCount.ToString("D2")));
			app.RemovePlateFromWorktable("Lynx", "Right", (destName+destCount.ToString("D2")));
			destCount++;
		}
		
		
	}
	public int SplitWorklist(string inputFile, string outputDir, int maxUniqueCol1, int maxUniqueCol3)
    {
        Directory.CreateDirectory(outputDir);
        var lines = File.ReadAllLines(inputFile);
        if (lines.Length == 0) return 0;
        string header = lines[0];
        int fileIndex = 1;

        var currentRows = new List<string>();
        var uniqueCol1 = new HashSet<string>();
        var uniqueCol3 = new HashSet<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            var columns = lines[i].Split(',');

            if (columns.Length < 3)
            {
                //Console.WriteLine($"Skipping malformed line {i + 1}");
                continue;
            }

            string col1 = columns[0].Trim();
            string col3 = columns[2].Trim();

            bool exceedsLimits = (uniqueCol1.Contains(col1) ? false : uniqueCol1.Count + 1 > maxUniqueCol1) ||
                                 (uniqueCol3.Contains(col3) ? false : uniqueCol3.Count + 1 > maxUniqueCol3);

            if (exceedsLimits)
            {
                WriteChunk(outputDir, fileIndex++, header, currentRows);
                currentRows.Clear();
                uniqueCol1.Clear();
                uniqueCol3.Clear();
            }

            currentRows.Add(lines[i]);
            uniqueCol1.Add(col1);
            uniqueCol3.Add(col3);
        }

        if (currentRows.Count > 0)
        {
            WriteChunk(outputDir, fileIndex, header, currentRows);
        }
		return fileIndex;
        //Console.WriteLine($"Split completed. Files saved in '{outputDir}'.");
    }
	
	public void ClearDirectory (string outputDir)
	{
		System.IO.DirectoryInfo di = new DirectoryInfo(outputDir);
		foreach (FileInfo file in di.EnumerateFiles())
		{
			file.Delete();
		}
		foreach (DirectoryInfo dir in di.EnumerateDirectories())
		{
			dir.Delete(true);
		}
	}
	
	public void WriteChunk(string dir, int index, string header, List<string> rows)
    {
        string filePath = (dir + index.ToString() + ".csv");
        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine(header);
            foreach (var row in rows)
            {
                writer.WriteLine(row);
            }
        }
    }
	
	
	public string AddZero(string well)
	{
	    string Let = well.Substring(0, 1);
	    int Num = Convert.ToInt32(well.Substring(1));
	    return Let + Num.ToString("D2");
	}
	public Dictionary<string, int> MakeMap()
	{
	    Dictionary<string, int> map = new Dictionary<string, int>();
	    int index = 1;
        for (int i = 1; i < 13; i++)
        {
		    for (char letter = 'A'; letter < 'I'; letter++)
		    {
	            map.Add(letter.ToString() + i.ToString("D2"), index);
	            index++;
	        }
	    }
	    return map;
	}
	public Dictionary<string, int> Create24Map()
	{
	    Dictionary<string, int> map = new Dictionary<string, int>();
	    int key = 1;
	    for (char l = 'A'; l <= 'D'; l++)
	    {
	        for (int j = 1; j < 7; j++)
	        {
	            map.Add(l.ToString() + j.ToString("D2"), key);
				key+=2;
	        }
			key+=12;
	    }
	    return map;
	}
}
public class Worklist
{
    public string SourceID { get; set; }
    public string SourceWell { get; set; }
    public string DestinationID { get; set; }
    public string DestinationWell { get; set; }
    public string Volume { get; set; }
    public string BufferVolume { get; set; }
    public string Centrifuge { get; set; }
}
