using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using dotless.Core;
using dotless.Core.configuration;
using Yahoo.Yui.Compressor;
using Microsoft.VisualStudio.CommandBars;
using Zippy.Chirp.Manager;
using Zippy.Chirp.Xml;
using VSLangProj;


namespace Zippy.Chirp
{
	/// <summary>The object for implementing an Add-in.</summary>
	/// <seealso class='IDTExtensibility2' />
	public class Chirp : IDTExtensibility2
	{
        DTE2 app;
        Events2 events;
        DocumentEvents eventsOnDocs;
        ProjectItemsEvents eventsOnProjectItems;
        SolutionEvents eventsOnSolution;
        BuildEvents eventsOnBuild;
        CommandEvents eventsOnCommand;
        AddIn instance;

        Dictionary<string, List<string>> dependentFiles = 
            new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

        const string regularCssFile = ".css";
        const string regularJsFile = ".js";
        const string regularLessFile = ".less";
        const string minifiedCssFile = ".min.css";
        const string minifiedJsFile = ".min.js";

		/// <summary>
        /// Implements the constructor for the Add-in object. Place your initialization code within this method.
        /// </summary>
		public Chirp()
		{
        }

		/// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
		/// <param term='application'>Root object of the host application.</param>
		/// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
		/// <param term='addInInst'>Object representing this Add-in.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
            Settings.Load();

            this.instance = addInInst as AddIn;
            this.app = application as DTE2;
            this.events = app.Events as Events2;

            object missing = System.Reflection.Missing.Value;
            this.eventsOnDocs = this.events.get_DocumentEvents(missing as Document);
            this.eventsOnProjectItems = this.events.ProjectItemsEvents;
            this.eventsOnSolution = this.events.SolutionEvents;
            this.eventsOnBuild = this.events.BuildEvents;
            this.eventsOnCommand = this.events.CommandEvents;

            this.eventsOnCommand.BeforeExecute += new _dispCommandEvents_BeforeExecuteEventHandler(CommandEvents_BeforeExecute);
            this.eventsOnBuild.OnBuildBegin  += new _dispBuildEvents_OnBuildBeginEventHandler(BuildEvents_OnBuildBegin);
            this.eventsOnSolution.Opened += new _dispSolutionEvents_OpenedEventHandler(SolutionEvents_Opened);
            this.eventsOnProjectItems.ItemRenamed += new _dispProjectItemsEvents_ItemRenamedEventHandler(ProjectItemsEvents_ItemRenamed);
            this.eventsOnProjectItems.ItemAdded += new _dispProjectItemsEvents_ItemAddedEventHandler(ProjectItemsEvents_ItemAdded);
            this.eventsOnDocs.DocumentSaved += new _dispDocumentEvents_DocumentSavedEventHandler(DocumentEvents_DocumentSaved);

            //OutputWindowWriteText("---Chirpy log---");
       }

        #region Unused IDTExtensibility2 methods
        /// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
		/// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
		}

		/// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />		
		public void OnAddInsUpdate(ref Array custom)
		{
		}

		/// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref Array custom)
		{
		}

		/// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref Array custom)
		{
        }
        #endregion

        #region Event Handlers
        void CommandEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            EnvDTE.Command objCommand = default(EnvDTE.Command);
            string sCommandName = null;

            objCommand = app.Commands.Item(Guid, ID);
            if ((objCommand != null))
            {
                sCommandName = objCommand.Name;

                if (Settings.T4RunAsBuild)
                {
                    System.Text.RegularExpressions.Regex buildCommand = new System.Text.RegularExpressions.Regex("Build.BuildSelection|Build.BuildSolution|ClassViewContextMenus.ClassViewProject.Build");
                    if (buildCommand.IsMatch(sCommandName))
                    {

                        try
                        {
                            string[] T4List = Settings.T4RunAsBuildTemplate.Split(new char[] { ',' });
                            foreach (string t4Template in T4List)
                            {
                                ProjectItem projectItem = app.Solution.FindProjectItem(t4Template.Trim());

                                if (projectItem != null)
                                {
                                    if (!projectItem.IsOpen)
                                        projectItem.Open();
                                    projectItem.Save();
                                }
                            }
                        }
                        catch (Exception eError)
                        {
                            OutputWindowWriteText("Error running template :" + eError.ToString());
                        }
                    }
                }
            }
        }


        void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            //code move to CommandEvents_BeforeExecute in this events show warning message
            //if (Settings.T4RunAsBuild)
            //{
            //    //run t4 template on build begin
            //    if (Scope == vsBuildScope.vsBuildScopeSolution || Scope == vsBuildScope.vsBuildScopeProject)
            //    {
                   
            //        try
            //        {
            //            string[] T4List=Settings.T4RunAsBuildTemplate.Split(new char[] { ',' });
            //            foreach (string t4Template in T4List)
            //            {
            //                ProjectItem projectItem = app.Solution.FindProjectItem(t4Template.Trim());

            //                if (projectItem != null)
            //                {
            //                    if (!projectItem.IsOpen)
            //                        projectItem.Open();
            //                    projectItem.Save();
            //                }
            //            }
            //        }
            //        catch(Exception eError)
            //        {
            //            OutputWindowWriteText("Error run template :" + eError.ToString());
            //        }
            //    }
            //}
        }

        void SolutionEvents_Opened()
        {
            foreach (Project project in this.app.Solution.Projects)
            {
                foreach (ProjectItem projectItem in ProcessAllProjectItemsRecursively(project.ProjectItems))
                {
                    if (IsConfigFile(projectItem.Name))
                    {
                        ReloadConfigFileDependencies(projectItem);
                    }
                }
            }
        }


        void ProjectItemsEvents_ItemAdded(ProjectItem projectItem)
        {
            string fileName = projectItem.Name;

            if (IsChirpLessFile(fileName))
            {
                GenerateCssFromLess(projectItem);
            }
            else if (IsChirpCssFile(fileName))
            {
                GenerateMinCssFromCss(projectItem);
            }
            else if (IsChirpYUIJsFile(fileName))
            {
                GenerateMinYUIJsFromJs(projectItem);
            }
            else if (IsChirpJsFile(fileName))
            {
                GenerateMinJsFromJs(projectItem, ClosureCompilerCompressMode.ADVANCED_OPTIMIZATIONS);
            }
            else if (IsChirpSimpleJsFile(fileName))
            {
                GenerateMinJsFromJs(projectItem, ClosureCompilerCompressMode.SIMPLE_OPTIMIZATIONS);
            }
            else if (IsChirpWhiteSpaceJsFile(fileName))
            {
                GenerateMinJsFromJs(projectItem, ClosureCompilerCompressMode.WHITESPACE_ONLY);
            }
        }

        void ProjectItemsEvents_ItemRenamed(ProjectItem projectItem, string oldFileName)
        {
            if (IsAnyChirpFile(projectItem.Name))
            {
                // Now a chirp file
                ProjectItemsEvents_ItemAdded(projectItem);
            }
            else if (IsAnyChirpFile(oldFileName))
            {
                try
                {
                    VSProjectItemManager.DeleteAllItems(projectItem.ProjectItems);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception was thrown when trying to rename file.\n" + e.ToString());
                }
            }
        }

        void DocumentEvents_DocumentSaved(Document document)
        {
            var projectItem = document.ProjectItem;
            string fileName = projectItem.Name;

            if (IsConfigFile(fileName))
            {
                GenerateFilesFromConfig(projectItem);
                ReloadConfigFileDependencies(projectItem);
            }
            else
            {
                if (IsAnyChirpFile(fileName))
                {
                    ProjectItemsEvents_ItemAdded(projectItem);
                }

                CheckForConfigRefreshRecursively(document.ProjectItem);
            }
        }
                
        void CheckForConfigRefreshRecursively(ProjectItem projectItem)
        {
            CheckForConfigRefresh(projectItem);
            foreach (ProjectItem projectItemInner in projectItem.ProjectItems)
            {
                CheckForConfigRefreshRecursively(projectItemInner);
            }
        }

        void CheckForConfigRefresh(ProjectItem projectItem)
        {
            string fullFileName = projectItem.get_FileNames(0);

            if (dependentFiles.ContainsKey(fullFileName))
            {
                foreach (string configFile in dependentFiles[fullFileName])
                {
                    ProjectItem item = LocateProjectItemForFileName(configFile);
                    if (item != null)
                    {
                        GenerateFilesFromConfig(item);
                    } 
                }
            }
        }
        #endregion

        #region File Dependencies
        /// <summary>
        /// build a dictionary that has the files that could change as the key.
        /// for the value it is a LIST of config files that need updated if it does change.
        /// so, when a .less.css file changes, we look in the list and rebuild any of the configs associated with it.
        /// if a config file changes...this rebuild all of this....
        /// </summary>
        /// <param name="projectItem"></param>
        void ReloadConfigFileDependencies(ProjectItem projectItem)
        {
            string configFileName = projectItem.get_FileNames(0);

            //remove all current dependencies for this config file...
            foreach (string key in dependentFiles.Keys)
            {
                List<string> files = dependentFiles[key];
                if (files.Remove(configFileName) && files.Count == 0)
                    dependentFiles.Remove(key);
            }

            var fileGroups = LoadConfigFileGroups(configFileName);
            foreach (var fileGroup in fileGroups)
            {
                foreach (var file in fileGroup.Files)
                {
                    if (!dependentFiles.ContainsKey(file.Path))
                    {
                        dependentFiles.Add(file.Path, new List<string> { configFileName });
                    }
                    else
                    {
                        dependentFiles[file.Path].Add(Settings.ChirpConfigFile);
                    }
                }
            }
        }

        IEnumerable<ProjectItem> ProcessAllProjectItemsRecursively(ProjectItems projectItems)
        {
            foreach (ProjectItem projectItem in projectItems)
            {
                if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder)
                {
                    foreach (ProjectItem folderProjectItem in ProcessAllProjectItemsRecursively(projectItem.ProjectItems))
                        yield return folderProjectItem;
                }

                yield return projectItem;
            }
        }

        ProjectItem LocateProjectItemForFileName(string fileName)
        {
            foreach (Project project in this.app.Solution.Projects)
            {
                foreach (ProjectItem projectItem in ProcessAllProjectItemsRecursively(project.ProjectItems))
                {
                    if (projectItem.get_FileNames(0) == fileName)
                        return projectItem;
                }

            }
            return null;
        }
        #endregion
      
        #region File Generation
        void GenerateMinYUIJsFromJs(ProjectItem projectItem)
        {
            try
            {
                string fileNamePrefix = GetFileNamePrefix(projectItem.Name, Settings.ChirpYUIJsFile);
                string js = File.ReadAllText(projectItem.get_FileNames(0));
                string miniJs = string.IsNullOrEmpty(js) ? string.Empty : TransformYUIJsToMiniJs(js);
               
                using (var manager = new VSProjectItemManager(app, projectItem, fileNamePrefix))
                {
                    manager.AddFile(minifiedJsFile, miniJs);
                }
            }
            catch (Exception e)
            {
               MessageBox.Show("Zippy Chirp Burped: Exception was thrown.\n" + e.ToString());
            }
        }

        void GenerateMinJsFromJs(ProjectItem projectItem, ClosureCompilerCompressMode compressMode)
        {
            try
            {
                string fileNamePrefix = string.Empty;
                switch (compressMode)
                {
                    case ClosureCompilerCompressMode.SIMPLE_OPTIMIZATIONS :
                        fileNamePrefix = GetFileNamePrefix(projectItem.Name, Settings.ChirpSimpleJsFile);
                        break;
                    case ClosureCompilerCompressMode.WHITESPACE_ONLY :
                        fileNamePrefix = GetFileNamePrefix(projectItem.Name, Settings.ChirpWhiteSpaceJsFile);
                        break;
                    default:
                        fileNamePrefix = GetFileNamePrefix(projectItem.Name, Settings.ChirpJsFile);
                        break;
                }

                string fileName = projectItem.get_FileNames(0);
                string miniJs = TransformFromClosureCompilerToMiniJs(fileName, compressMode);
               using (var manager = new VSProjectItemManager(app, projectItem, fileNamePrefix))
                {
                    manager.AddFile(minifiedJsFile, miniJs);
                }
            }
            catch (Exception e)
            {
                OutputWindowWriteText("Zippy Chirp Burped: Exception was thrown.\n" + e.ToString());
                MessageBox.Show("Zippy Chirp Burped: Exception was thrown.\n" + e.ToString());
            }
        }

        void GenerateMinCssFromCss(ProjectItem projectItem)
        {
            try
            {
                string fileNamePrefix = GetFileNamePrefix(projectItem.Name,Settings.ChirpCssFile);
                string css = File.ReadAllText(projectItem.get_FileNames(0));
                string miniCss = string.IsNullOrEmpty(css) ? string.Empty : TransformCssToMiniCss(css);

                using (var manager = new VSProjectItemManager(app, projectItem, fileNamePrefix))
                {
                    manager.AddFile(minifiedCssFile, miniCss);
                }
            }
            catch (Exception e)
            {
                OutputWindowWriteText("Zippy Chirp Burped: Exception was thrown.\n" + e.ToString());
                MessageBox.Show("Zippy Chirp Burped: Exception was thrown.\n" + e.ToString());
            }            
        }

        void GenerateCssFromLess(ProjectItem projectItem)
        {
            try
            {
                string fileNamePrefix = GetFileNamePrefix(projectItem.Name, Settings.ChirpLessFile);
                string filePath = projectItem.get_FileNames(0);
                string fileText = File.ReadAllText(filePath);

                string css = TransformLessToCss(filePath, fileText);
                string miniCss = string.IsNullOrEmpty(css) ? string.Empty : TransformCssToMiniCss(css);

                using (var manager = new VSProjectItemManager(app, projectItem, fileNamePrefix))
                {
                    manager.AddFile(regularCssFile, css);
                    manager.AddFile(minifiedCssFile, miniCss);
                }
            }
            catch (Exception e)
            {
                OutputWindowWriteText("Zippy Chirp Burped: Exception was thrown.\n" + e.ToString());
                MessageBox.Show("Zippy Chirp Burped: Exception was thrown.\n" + e.ToString());
            }
        }
        #endregion

        #region Config Files
        IList<FileGroupXml> LoadConfigFileGroups(string configFileName)
        {
            XDocument doc = XDocument.Load(configFileName);

            string appRoot = string.Format("{0}\\", Path.GetDirectoryName(configFileName));
            return doc.Descendants("FileGroup")
                .Select(n => new FileGroupXml(n, appRoot))
                .ToList();
        }

        void GenerateFilesFromConfig(ProjectItem projectItem)
        {
            try
            {
                var fileGroups = LoadConfigFileGroups(projectItem.get_FileNames(0));
                string directory = Path.GetDirectoryName(projectItem.get_FileNames(0));

                using (var manager = new VSProjectItemManager(this.app, projectItem))
                {
                    foreach (var fileGroup in fileGroups)
                    {
                        var allFileText = new StringBuilder();
                        foreach (var file in fileGroup.Files)
                        {
                            string path = file.Path;
                            string text = File.ReadAllText(path);

                            if (IsLessFile(path))
                            {
                                text = TransformLessToCss(path, text);
                            }

                            if (file.Minify)
                            {
                                if (IsCssFile(path) || IsLessFile(path))
                                {
                                    text = TransformCssToMiniCss(text);
                                }
                                else if (IsJsFile(path))
                                {
                                    text = TransformJsToMiniJs(text);
                                }
                            }

                            allFileText.Append(text);
                            allFileText.Append(Environment.NewLine);
                        }

                        string fullPath = directory + @"\" + fileGroup.Name;
                        manager.AddFileByFileName(fullPath, allFileText.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Zippy Chirp Burped: Exception was thrown.\n" + e.ToString());
            }
        }
        #endregion

        #region IsChirpFile
        bool IsChirpLessFile(string fileName)
        {
            return (fileName.EndsWith(Settings.ChirpLessFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsLessFile(string fileName)
        {
            return (fileName.EndsWith(regularLessFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsChirpCssFile(string fileName)
        {
            return (fileName.EndsWith(Settings.ChirpCssFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsCssFile(string fileName)
        {
            return (fileName.EndsWith(regularCssFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsChirpYUIJsFile(string fileName)
        {
            return (fileName.EndsWith(Settings.ChirpYUIJsFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsChirpJsFile(string fileName)
        {
            return (fileName.EndsWith(Settings.ChirpJsFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsChirpWhiteSpaceJsFile(string fileName)
        {
            return (fileName.EndsWith(Settings.ChirpWhiteSpaceJsFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsChirpSimpleJsFile(string fileName)
        {
            return (fileName.EndsWith(Settings.ChirpSimpleJsFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsJsFile(string fileName)
        {
            return (fileName.EndsWith(regularJsFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsConfigFile(string fileName)
        {
            return (fileName.EndsWith(Settings.ChirpConfigFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsAnyChirpFile(string fileName)
        {
            return IsChirpLessFile(fileName) || IsChirpCssFile(fileName) || IsChirpJsFile(fileName) || IsChirpSimpleJsFile(fileName) || IsChirpWhiteSpaceJsFile(fileName) || IsChirpYUIJsFile(fileName);
        }

        string GetFileNamePrefix(string fileName, string suffix)
        {
            int PosSuffix=fileName.ToLower().IndexOf(suffix.ToLower());
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("Invalid file name: " + fileName);
            if (PosSuffix >= 0)
                return fileName.Substring(0, PosSuffix);
            else
                return string.Empty;
        }
        #endregion

        #region Less Engine
        ILessEngine lazyLessEngine;
        ILessEngine lessEngine
        {
            get
            {
                if (lazyLessEngine == null)
                {
                    lazyLessEngine = new EngineFactory().GetEngine(new DotlessConfiguration
                    {
                        MinifyOutput = false
                    });
                }
                return lazyLessEngine;
            }
        }
        #endregion

        #region Transformations
        string TransformLessToCss(string fullFileName, string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return string.Empty;

                LessSourceObject lessFile = new LessSourceObject
                {
                    Key = fullFileName,
                    Content = text
                };

                var current = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(new FileInfo(fullFileName).DirectoryName);

                var ret = this.lessEngine.TransformToCss(lessFile);

                Directory.SetCurrentDirectory(current);
                return ret;
            }
            catch (Exception e)
            {
                string msg = "Error Generating Ouput: " + Environment.NewLine + e.Message;
                string commentedMessage = "//" + msg.Replace(Environment.NewLine, Environment.NewLine + "//");
                OutputWindowWriteText(commentedMessage);
               return commentedMessage;
            }
        }

        string TransformCssToMiniCss(string text)
        {
            try
            {
                return CssCompressor.Compress(text);
            }
            catch (Exception e)
            {
                string msg = "Error Generating Ouput: " + Environment.NewLine + e.Message;
                string commentedMessage = "//" + msg.Replace(Environment.NewLine, Environment.NewLine + "//");
                OutputWindowWriteText(commentedMessage);
                return commentedMessage;
            }
        }

        string TransformYUIJsToMiniJs(string text)
        {
            try
            {
                return JavaScriptCompressor.Compress(text);
            }
            catch (Exception e)
            {
                string msg = "Error Generating Ouput: " + Environment.NewLine + e.Message;
                string commentedMessage = "//" + msg.Replace(Environment.NewLine, Environment.NewLine + "//");
                OutputWindowWriteText(commentedMessage);
                return commentedMessage;
            }
        }

        string TransformFromClosureCompilerToMiniJs(string fileName, ClosureCompilerCompressMode compressMode)
        {
            try
            {
                return GoogleClosureCompiler.Compress(fileName, compressMode);
            }
            catch (Exception e)
            {
                string msg = "Error Generating Ouput: " + Environment.NewLine + e.Message;
                string commentedMessage = "//" + msg.Replace(Environment.NewLine, Environment.NewLine + "//");
                OutputWindowWriteText(commentedMessage);
                return commentedMessage;
            }
        }

        string TransformJsToMiniJs(string text)
        {
            try
            {
                return JavaScriptCompressor.Compress(text);
            }
            catch (Exception e)
            {
                string msg = "Error Generating Ouput: " + Environment.NewLine + e.Message;
                string commentedMessage = "//" + msg.Replace(Environment.NewLine, Environment.NewLine + "//");
                OutputWindowWriteText(commentedMessage);
                return commentedMessage;
            }
        }
        #endregion

        #region "output login"
        private void OutputWindowWriteText(string messageText)
        {
            try
            {
                OutputWindow ow = this.app.ToolWindows.OutputWindow;
                OutputWindowPane owP;

                try
                {
                    owP = ow.OutputWindowPanes.Item("Chirpy");
                }
                catch
                {
                    owP = ow.OutputWindowPanes.Add("Chirpy");
                }
                owP.Activate();
                // Add a line of text to the new pane.
                owP.OutputString(messageText + System.Environment.NewLine );
            }
            catch (Exception eError)
            {
                MessageBox.Show(eError.ToString());
            }
        }

        #endregion

    }
}