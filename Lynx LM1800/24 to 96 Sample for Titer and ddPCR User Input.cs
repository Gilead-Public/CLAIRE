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
		app.SetVariable("VMDIList", "");
		app.SetVariable("VIList", "");
		app.SetVariable("SourceList", "");
		app.SetVariable("SourceLidList", "");
		app.SetVariable("DestList", "");
		app.SetVariable("DestLidList", "");
		app.SetVariable("InputFile", "");							
		app.SetVariable("SourceCodes", "");
		app.SetVariable("DestCodes", "");
		app.SetVariable("SourceType", "24Src");
		app.SetVariable("SourceLabware", "24 DWP Enzyscreen");
		app.SetVariable("DestType", "96Dest");
		app.SetVariable("DestLabware", "96 DWP Greiner BioOne");
		app.SetVariable("SourceLidOffset", "-33");
		app.SetVariable("DestLidOffset", "0");
		app.SetVariable("SourceCount", "");
		app.SetVariable("DestCount", "");
		
       	string outputDir = "C:\\MethodManager4\\Workspaces\\LO5004\\Temp Worklists\\";
       	int maxUniqueCol1 = 16;   // Limit for column 1
       	int maxUniqueCol3 = 4;  // Limit for column 3
		int maxTips = 4;
		int destCount = 0;
		int sourceCount = 0;
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
					SampleID = line[4]
		       	});
	   		}
		}
		
		// Create VIs and VMDIs
		var groupedBySource = worklist.GroupBy(x => x.SourceID);
		app.SetVariable("SourceCount", groupedBySource.Count());
		List<string> sourceIDs = new List<string>();
		List<string> sourceLidIDs = new List<string>();
		List<string> destSamplePairs = new List<string>();

		string[] aspVI = new string[97];
		string[] dispVMDI = new string[97];
		List<string> finalAspVI = new List<string>();
		List<string> finalDispVMDI = new List<string>();
		var map = Create24Map();
		int index = 0;
		int volume = 250;
		double dispVol = 0;
			
		//Transfer VI and VMDI Generation
		foreach (var source in groupedBySource)
		{
			
			aspVI[0] = "+VI;12;8";
			dispVMDI[0] = "+VMDI;12;8";
			
			sourceIDs.Add("," + source.Key);
			sourceLidIDs.Add("," + source.Key + ".Lid");
				
			foreach (var well in source)
			{
				index = map[well.SourceWell];
				dispVol = volume * 1.3;

				aspVI[index] = volume.ToString() + ";;20";
				dispVMDI[index] = well.DestinationID + ":" + well.DestinationWell + ";" + dispVol.ToString() + ";;20";
			}
		    finalAspVI.Add(string.Join(",", aspVI));
		    finalDispVMDI.Add(string.Join(",", dispVMDI));

			aspVI = new string[97];
			dispVMDI = new string[97];
		}
			
		//Destination List Generation
		var groupedByDest = worklist.GroupBy(x => x.DestinationID);
		app.SetVariable("DestCount",groupedByDest.Count());
		List<string> destIDs = new List<string>();
		List<string> destLidIDs = new List<string>();
		
		foreach (var dest in groupedByDest)
		{
			destIDs.Add("," + dest.Key);
			destLidIDs.Add("," + dest.Key + ".Lid");
		}
		//Titer plate List Generation
		var groupedBySampleID = worklist.GroupBy(x=>x.SampleID);
		List<string>sampleIDs = new List<string>();
		foreach (var id in groupedBySampleID)
		{
			sampleIDs.Add("," + id.Key);
		}
		
		
		// Set variables
		app.SetVariable("VIList", string.Join("", finalAspVI));
		app.SetVariable("VMDIList", string.Join("", finalDispVMDI));
		app.SetVariable("SourceList", string.Join("", sourceIDs));
		app.SetVariable("SourceLidList", string.Join("", sourceLidIDs));
		app.SetVariable("DestList", string.Join("", destIDs));
		app.SetVariable("DestLidList", string.Join("", destLidIDs));
		app.SetVariable("SecondDestList",string.Join("",sampleIDs));
			
		string message = ("This is worklist " + app.GetVariableValue("WorklistIndex") + " of " + app.GetVariableValue("WorklistsInSet") + " in this set. Source Barcodes in this set are \r\n" + app.GetVariableValue("SourceList") + ",\r\n ddPCR Barcodes in this set are \r\n" + app.GetVariableValue("DestList") +", and \r\n Titer Barcodes in this set are \r\n" + app.GetVariableValue("SecondDestList") + ".");
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
		string tipsName = "TransferTips_";
		sourceCount = int.Parse(app.GetVariableValue("SourceCount"))+1;	
		destCount = int.Parse(app.GetVariableValue("DestCount"))+1;
		double tipCount = groupedBySource.Count();	
		int tipBoxes = (int)Math.Ceiling(tipCount/4)+1;
		
	
		while (tipBoxes<=maxTips)
		{
			app.RemovePlateFromWorktable("Lynx", "Left",(tipsName+tipBoxes.ToString("D2")));
			tipBoxes++;
		}
		
		while (sourceCount<=maxUniqueCol1)
		{
			app.RemovePlateFromWorktable("Lynx", "Left",(sourceName+sourceCount.ToString("D2")));
			sourceCount++;	
		}
		
		while (destCount<=maxUniqueCol3)
		{
			app.RemovePlateFromWorktable("Lynx", "Left", (destName+destCount.ToString("D2")));
			app.RemovePlateFromWorktable("Lynx", "Left", ("96Titer_"+destCount.ToString("D2")));
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
	    for (char letter = 'A'; letter < 'I'; letter++)
	    {
	        for (int i = 1; i < 13; i++)
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
	    int index = 1;
	    for (char letter = 'A'; letter < 'E'; letter++)
	    {
	        for (int i = 1; i < 7; i++)
	        {
	            map.Add(letter.ToString() + i.ToString("D2"), index);
	            index+=2;
	        }
			index+=12;
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
	public string SampleID { get; set; }
}
