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

		// Pull in scanned barcodes and source list
		var list1 = app.GetVariableValue("SourceCodes").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		var list2 = app.GetVariableValue("SourceList").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

		// Find mismatches
		var scanned = list1.Except(list2).ToArray();
		var listed = list2.Except(list1).ToArray();
		if (scanned.Length>0)
		{
			var msg = "The following barcodes were scanned but not found in the worklist.\n" + string.Join(", ", scanned);
			throw new Exception(msg);
		}
		if (listed.Length>0)
		{
			var msg2 = "The following barcodes are in the worklist but were not scanned.\n" + string.Join(", ", listed);
			throw new Exception(msg2);
		}
	}
}
		