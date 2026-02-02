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
		app.SetVariable("DestCount", "");
		app.SetVariable("PlateList", "");
		app.SetVariable("LidList", "");
		app.SetVariable("DestCodes","");

		app.SetVariable("SourceLocations", "");
		app.SetVariable("SourceLidLocations","");
		app.SetVariable("SourceLidOffset", "");
		app.SetVariable("DestLidOffset", "-7");
		app.SetVariable("SourceList","");
		
		//User Dialog
		MMScriptDialog Window1 = new MMScriptDialog();
		Window1.AddOpenFileDlg("Select Worklist", "", "csv", true, "InputFile");
		bool proceed = Window1.Show(app, "User Input", false, true);
		
		if (proceed)
		{
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
			            PlateID = line[0],

		        	});
	   			}
			}
			
			var groupedByPlate = worklist.GroupBy(x => x.PlateID);
			
			app.SetVariable("DestCount", groupedByPlate.Count());
			List<string> destIDs = new List<string>();
			List<string> destLidIDs = new List<string>();
			
			foreach (var dest in groupedByPlate)
			{
			destIDs.Add("," + dest.Key);
			destLidIDs.Add("," + dest.Key + ".Lid");	
			}
			
			app.SetVariable("DestList", string.Join("", destIDs));
			app.SetVariable("DestLidList", string.Join("", destLidIDs));
			
			string methodName = app.GetVariableValue("MethodName");
			string plateName = "96WP_";
			app.SetVariable("DestType","96WP");
			string lidName = "96WPLid_";
			string tipsName = "MediaTips_";
			int plateCount = int.Parse(app.GetVariableValue("DestCount"))+1;	

			while (plateCount<=20)
			{
				app.RemovePlateFromWorktable("Lynx", "Left",(plateName+plateCount.ToString("D2")));
				
				if (methodName == "Day 14 Colony Imaging" || methodName == "Day 09 Colony Imaging")
				{
				app.RemovePlateFromWorktable("Lynx", "Left",(tipsName+plateCount.ToString("D2")));
				}
				plateCount++;	
			}
		}
	}
}
public class Worklist
{
    public string PlateID { get; set; }

}