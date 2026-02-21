// ===================================================================================
// File: CodeExporterWindow.cs
// Description: Export project code files into a single text file.
//              Features:
//              - Dynamic Path based on Project Settings (Company/Product Name).
//              - Tight UI layout (Removed unwanted gaps).
//              - Unity 6 Compatible.
// ===================================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Bussuf.Utilities.Editor
{
	public class CodeExporterWindow : EditorWindow
	{
		// --- Configuration ---
		private static readonly HashSet<string> _allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			".cs", ".js", ".json", ".py", ".shader", ".cginc", ".hlsl", ".compute", ".asmdef",
			".xml", ".html", ".css", ".cpp", ".h", ".c", ".txt", ".md"
		};

		// --- Fields ---
		private FileNode _rootNode;
		private Vector2 _scrollPosition;
		private int _totalFilesFound = 0;

		// --- Path Logic ---
		// Uses Unity's PersistentDataPath which is based on Project Settings > Player > Company Name / Product Name
		// This ensures every project has its own unique export folder automatically.
		private string GetProjectExportPath()
		{
			// Result example: C:\Users\Assaf\AppData\LocalLow\BussufGames\Chesslord\CodeExports
			return Path.Combine(Application.persistentDataPath, "CodeExports");
		}

		// --- Inner Class ---
		private class FileNode
		{
			public string Name;
			public string FullPath;
			public string RelativePath;
			public bool IsFolder;
			public bool IsSelected = true;
			public bool IsExpanded = true;
			public List<FileNode> Children = new List<FileNode>();

			public void SetSelectionRecursive(bool selected)
			{
				IsSelected = selected;
				foreach (var child in Children) child.SetSelectionRecursive(selected);
			}
		}

		[MenuItem("Bussuf+/Tools/Code Exporter")]
		public static void ShowWindow()
		{
			var window = GetWindow<CodeExporterWindow>("Code Exporter");
			window.minSize = new Vector2(350, 400);
			window.Show();
		}

		private void OnEnable()
		{
			RefreshFileTree();
		}

		private void OnGUI()
		{
			// 1. Header (Fixed Height)
			GUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.Label("Code Exporter", EditorStyles.boldLabel);
			if (GUILayout.Button("Refresh File List", GUILayout.Height(25)))
			{
				RefreshFileTree();
			}
			GUILayout.Label($"Found {_totalFilesFound} exportable files in project.", EditorStyles.miniLabel);
			GUILayout.EndVertical();

			// 2. Tree View (Takes remaining space, tightly packed)
			// 'GUIStyle.none' removes the default box padding causing gaps
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUIStyle.none);

			if (_rootNode != null && _rootNode.Children.Count > 0)
			{
				DrawNode(_rootNode, 0);
			}
			else
			{
				GUILayout.Label("No code files found matching extensions.", EditorStyles.centeredGreyMiniLabel);
			}

			EditorGUILayout.EndScrollView();

			// Separator line
			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

			// 3. Footer / Action (Pinned to Bottom)
			GUILayout.BeginVertical();
			GUI.backgroundColor = new Color(0.2f, 0.85f, 0.2f); // Green

			if (GUILayout.Button("Export Files (Auto-Save)", GUILayout.Height(40)))
			{
				ExportSelectedFiles();
			}

			GUI.backgroundColor = Color.white;

			// Show the path to the user so they know where it's going
			GUILayout.Label($"Saves to: {GetProjectExportPath()}", EditorStyles.miniLabel);
			GUILayout.EndVertical();
		}

		// --- Logic: Tree Building ---
		private void RefreshFileTree()
		{
			_totalFilesFound = 0;
			_rootNode = new FileNode
			{
				Name = "Assets",
				FullPath = Application.dataPath,
				IsFolder = true,
				IsSelected = true
			};

			BuildTreeRecursive(Application.dataPath, _rootNode);
		}

		private void BuildTreeRecursive(string currentPath, FileNode parentNode)
		{
			string[] files = Directory.GetFiles(currentPath);
			foreach (string file in files)
			{
				if (_allowedExtensions.Contains(Path.GetExtension(file)))
				{
					parentNode.Children.Add(new FileNode
					{
						Name = Path.GetFileName(file),
						FullPath = file,
						RelativePath = file.Replace(Application.dataPath, "Assets").Replace("\\", "/"),
						IsFolder = false,
						IsSelected = parentNode.IsSelected
					});
					_totalFilesFound++;
				}
			}

			string[] directories = Directory.GetDirectories(currentPath);
			foreach (string dir in directories)
			{
				if (new DirectoryInfo(dir).Name.StartsWith(".")) continue;

				FileNode dirNode = new FileNode
				{
					Name = Path.GetFileName(dir),
					FullPath = dir,
					IsFolder = true,
					IsSelected = parentNode.IsSelected
				};

				BuildTreeRecursive(dir, dirNode);

				if (dirNode.Children.Count > 0)
				{
					parentNode.Children.Add(dirNode);
				}
			}
		}

		// --- Logic: Drawing UI ---
		private void DrawNode(FileNode node, int indentLevel)
		{
			if (node == _rootNode)
			{
				foreach (var child in node.Children) DrawNode(child, 0);
				return;
			}

			// Using Horizontal layout with no spacing options to keep it tight
			EditorGUILayout.BeginHorizontal();

			// Native Unity indent logic often looks better than manual Space
			GUILayout.Space(indentLevel * 15);

			if (node.IsFolder)
			{
				node.IsExpanded = EditorGUILayout.Foldout(node.IsExpanded, "", true);

				// Tight label/toggle
				EditorGUI.BeginChangeCheck();
				bool newSelect = EditorGUILayout.ToggleLeft(node.Name, node.IsSelected, EditorStyles.boldLabel);
				if (EditorGUI.EndChangeCheck()) node.SetSelectionRecursive(newSelect);

				EditorGUILayout.EndHorizontal(); // End row before drawing children

				if (node.IsExpanded)
				{
					foreach (var child in node.Children) DrawNode(child, indentLevel + 1);
				}
			}
			else
			{
				// File row
				node.IsSelected = EditorGUILayout.ToggleLeft(node.Name, node.IsSelected);
				EditorGUILayout.EndHorizontal();
			}
		}

		// --- Logic: Exporting ---
		private void ExportSelectedFiles()
		{
			List<FileNode> filesToExport = new List<FileNode>();
			CollectSelectedFiles(_rootNode, filesToExport);

			if (filesToExport.Count == 0)
			{
				EditorUtility.DisplayDialog("Info", "No files selected.", "OK");
				return;
			}

			string targetDir = GetProjectExportPath();
			string fileName = $"CodeExport_{Application.productName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
			string fullPath = Path.Combine(targetDir, fileName);

			try
			{
				if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

				using (StreamWriter writer = new StreamWriter(fullPath, false, Encoding.UTF8))
				{
					writer.WriteLine($"===================================================================");
					writer.WriteLine($"PROJECT: {Application.productName} ({Application.companyName})");
					writer.WriteLine($"DATE:    {DateTime.Now}");
					writer.WriteLine($"FILES:   {filesToExport.Count}");
					writer.WriteLine($"===================================================================\n");

					writer.WriteLine("--- File Index ---");
					foreach (var f in filesToExport) writer.WriteLine($"- {f.RelativePath}");
					writer.WriteLine("\n===================================================================\n");

					foreach (var fileNode in filesToExport)
					{
						writer.WriteLine($"// FILE: {fileNode.RelativePath}");
						writer.WriteLine("-------------------------------------------------------------------");
						writer.Write(File.ReadAllText(fileNode.FullPath));
						writer.WriteLine("\n\n");
					}
				}

				Debug.Log($"[CodeExporter] Exported to: {fullPath}");

				if (EditorUtility.DisplayDialog("Export Complete",
					$"Saved to project folder:\n{targetDir}",
					"Open Folder", "Close"))
				{
					EditorUtility.RevealInFinder(fullPath);
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"Export failed: {e.Message}");
				EditorUtility.DisplayDialog("Error", "Export failed. Check console.", "OK");
			}
		}

		private void CollectSelectedFiles(FileNode node, List<FileNode> collection)
		{
			if (node.IsFolder)
			{
				foreach (var child in node.Children) CollectSelectedFiles(child, collection);
			}
			else if (node.IsSelected)
			{
				collection.Add(node);
			}
		}
	}
}
#endif