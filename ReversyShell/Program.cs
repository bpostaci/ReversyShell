using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Disassembler;
using System.Threading;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.DebugInfo;

using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler;
using System.Drawing;
using con = Colorful.Console;
using System.Reflection;

namespace ReversyShell
{
	
	class ReversyShellProgram
	{
		
		public static int Main(string[] args)
		{
			int exitCode = ProgramExitCodes.EX_SUCCESS;
			Version version = Assembly.GetEntryAssembly().GetName().Version;
			var vstring = "v"+ version.Major.ToString() + "." + version.Minor.ToString(); 
			Colorful.Console.WriteAscii($"Reversy {vstring}", Colorful.FigletFont.Default, Color.Purple);
#if DEBUG
			con.WriteLine("!Debug version Enabled! Do not use in production !", Color.Yellow);
#endif
			con.WriteLine("by bpostaci", Color.Cyan);
			con.WriteLine();

			if (args.Length != 1)
			{
				PrintUsage();
				return ProgramExitCodes.EX_USAGE; 
			}


			ConfigParser cparser = new ConfigParser(args[0]);
			var result = cparser.ParseConfig();
			if (result.IsFailure)           //Just small program logic no need to refactor as tail pattern.
			{
				con.WriteLine("Error :" + result.Error, Color.Red);
				exitCode = ProgramExitCodes.EX_DATAERR;
			}
			else
			{
				Decompiler engine = new Decompiler(cparser);
				var eResult = engine.Execute();
				if (eResult.IsFailure)
				{
					con.WriteLine("Error :" + result.Error, Color.Red);
					exitCode = ProgramExitCodes.EX_DATAERR;
				}
			}
			
			con.WriteLine(""); 
			con.WriteLine("Bye Bye!",Color.Cyan);
#if DEBUG
			Console.ReadKey(); 
#endif
			return exitCode; 
		}


		public static void PrintUsage()
        {
			con.WriteLine("ReversyShell is an ICSharpCode.Decompiler based fork that design and customized to works with windbg Mex extension \n to generate managed pdbs and psedo source codes that reverse engineered projects");
			con.WriteLine(); 
			con.WriteLine("Usage:", Color.Yellow);
			con.WriteLine(" reversyshell.exe <path of config>"); 

        }

	
	}
}
