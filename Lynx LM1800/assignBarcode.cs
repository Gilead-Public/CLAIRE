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
		var scanLoc = app.GetVariableValue("ScanLoc");
		var plateNames = app.QueryLocationStack("Lynx", "Right", scanLoc);
		var plateName = plateNames[0];
		var insertIndex = plateName.Length - 3;
		var lidName = plateName.Insert(insertIndex,"Lid");
		var barcode = app.GetVariableValue("Lynx.SR710.Barcodes");
		

		app.SetBarcode("Lynx", "Left", plateName, barcode);
		app.SetBarcode("Lynx", "Right", plateName, barcode);
		app.SetBarcode("Lynx", "Left", lidName, barcode+".Lid");
		app.SetBarcode("Lynx", "Right", lidName, barcode+".Lid");
		
	}
}
