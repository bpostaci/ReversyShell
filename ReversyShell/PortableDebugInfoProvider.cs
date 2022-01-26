using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.DebugInfo;

namespace ReversyShell
{
	class PortableDebugInfoProvider : IDebugInfoProvider
	{
		string pdbFileName;
		string moduleFileName;
		readonly MetadataReaderProvider provider;
		bool hasError;

		internal bool IsEmbedded => pdbFileName == null;

		public PortableDebugInfoProvider(string pdbFileName, MetadataReaderProvider provider,
			string moduleFileName)
		{
			this.pdbFileName = pdbFileName;
			this.moduleFileName = moduleFileName;
			this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		public string Description
		{
			get
			{
				if (pdbFileName == null)
				{
					if (hasError)
						return "Error while loading the PDB stream embedded in this assembly";
					return "Embedded in this assembly";
				}
				else
				{
					if (hasError)
						return $"Error while loading portable PDB: {pdbFileName}";
					return $"Loaded from portable PDB: {pdbFileName}";
				}
			}
		}

		internal MetadataReader GetMetadataReader()
		{
			try
			{
				hasError = false;
				return provider.GetMetadataReader();
			}
			catch (BadImageFormatException)
			{
				hasError = true;
				return null;
			}
			catch (IOException)
			{
				hasError = true;
				return null;
			}
		}

		public string SourceFileName => pdbFileName ?? moduleFileName;

		public IList<ICSharpCode.Decompiler.DebugInfo.SequencePoint> GetSequencePoints(MethodDefinitionHandle method)
		{
			var metadata = GetMetadataReader();
			var debugInfo = metadata.GetMethodDebugInformation(method);
			var sequencePoints = new List<ICSharpCode.Decompiler.DebugInfo.SequencePoint>();
			if (metadata == null)
				return sequencePoints;

			foreach (var point in debugInfo.GetSequencePoints())
			{
				string documentFileName;

				if (!point.Document.IsNil)
				{
					var document = metadata.GetDocument(point.Document);
					documentFileName = metadata.GetString(document.Name);
				}
				else
				{
					documentFileName = "";
				}

				sequencePoints.Add(new ICSharpCode.Decompiler.DebugInfo.SequencePoint()
				{
					Offset = point.Offset,
					StartLine = point.StartLine,
					StartColumn = point.StartColumn,
					EndLine = point.EndLine,
					EndColumn = point.EndColumn,
					DocumentUrl = documentFileName
				});
			}

			return sequencePoints;
		}

		public IList<Variable> GetVariables(MethodDefinitionHandle method)
		{
			var metadata = GetMetadataReader();
			var variables = new List<Variable>();
			if (metadata == null)
				return variables;

			foreach (var h in metadata.GetLocalScopes(method))
			{
				var scope = metadata.GetLocalScope(h);
				foreach (var v in scope.GetLocalVariables())
				{
					var var = metadata.GetLocalVariable(v);
					variables.Add(new Variable(var.Index, metadata.GetString(var.Name)));
				}
			}

			return variables;
		}

		public bool TryGetName(MethodDefinitionHandle method, int index, out string name)
		{
			var metadata = GetMetadataReader();
			name = null;
			if (metadata == null)
				return false;

			foreach (var h in metadata.GetLocalScopes(method))
			{
				var scope = metadata.GetLocalScope(h);
				foreach (var v in scope.GetLocalVariables())
				{
					var var = metadata.GetLocalVariable(v);
					if (var.Index == index)
					{
						name = metadata.GetString(var.Name);
						return true;
					}
				}
			}
			return false;
		}

       
    }
}
