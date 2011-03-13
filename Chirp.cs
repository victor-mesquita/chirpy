using System;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using Zippy.Chirp.Engines;
using Zippy.Chirp.Manager;

namespace Zippy.Chirp
{
    /// <summary>
    /// The object for implementing an Add-in.
    /// </summary>
    /// <seealso cref="IDTExtensibility2"/>
    public class Chirp : IDTExtensibility2
    {
        internal DTE2 app;
        private Events2 events;
        private DocumentEvents eventsOnDocs;
        private ProjectItemsEvents eventsOnProjectItems;
        private SolutionEvents eventsOnSolution;
        private BuildEvents eventsOnBuild;
        private CommandEvents eventsOnCommand;
        private AddIn instance;
        private TaskList tasks;
        private OutputWindowPane lazyOutputWindowPane;


        private EngineManager engineManager;

        internal EngineManager EngineManager
        {
            get
            {
                if (this.engineManager == null || this.engineManager.IsDisposed)
                {
                    this.LoadActions();
                }

                return this.engineManager;
            }
        }

        internal YuiJsEngine YuiJsEngine { get; set; }

        internal YuiCssEngine YuiCssEngine { get; set; }

        internal ClosureCompilerEngine ClosureCompilerEngine { get; set; }

        internal MsCssEngine MsCssEngine { get; set; }

        internal MsJsEngine MsJsEngine { get; set; }

        internal LessEngine LessEngine { get; set; }

        internal ConfigEngine ConfigEngine { get; set; }

        internal T4Engine T4Engine { get; set; }

        internal ViewEngine ViewEngine { get; set; }

        internal CoffeeScriptEngine CoffeeScriptEngine { get; set; }

        internal UglifyEngine UglifyEngine { get; set; }

        internal JSHintEngine JSHintEngine { get; set; }

        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            Settings.Load();
            Settings.Saved += this.LoadActions;

            this.instance = addInInst as AddIn;
            this.app = application as DTE2;
            this.events = this.app.Events as Events2;
        }

        public void LoadActions()
        {
            if (this.engineManager == null || this.engineManager.IsDisposed)
            {
                this.engineManager = new EngineManager(this);
            }

            this.engineManager.Clear();
            this.engineManager.Add(YuiCssEngine = new YuiCssEngine());
            this.engineManager.Add(YuiJsEngine = new YuiJsEngine());
            this.engineManager.Add(ClosureCompilerEngine = new ClosureCompilerEngine());
            this.engineManager.Add(LessEngine = new LessEngine());
            this.engineManager.Add(MsJsEngine = new MsJsEngine());
            this.engineManager.Add(MsCssEngine = new MsCssEngine());
            this.engineManager.Add(ConfigEngine = new ConfigEngine());
            this.engineManager.Add(ViewEngine = new ViewEngine());
            this.engineManager.Add(T4Engine = new T4Engine());
            this.engineManager.Add(CoffeeScriptEngine = new CoffeeScriptEngine());
            this.engineManager.Add(UglifyEngine = new UglifyEngine());
            this.engineManager.Add(JSHintEngine = new JSHintEngine());
        }

        void EventsOnSolution_AfterClosing()
        {
            if (this.tasks != null) this.tasks.RemoveAll();
        }

        void SolutionEvents_ProjectRemoved(Project Project)
        {
            this.tasks.Remove(Project);
        }

        /// <summary>
        /// Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.
        /// </summary>
        /// <param name="disconnectMode">Describes how the Add-in is being unloaded.</param>
        /// <param name="custom">Array of parameters that are host application specific.</param>
        /// <seealso cref="IDTExtensibility2"/>
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
            EngineManager.Dispose();
        }

        #region Unused IDTExtensibility2 methods

        /// <summary>
        /// Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.
        /// </summary>
        /// <param name="custom">Array of parameters that are host application specific.</param>
        /// <seealso cref="IDTExtensibility2"/>
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        /// <summary>
        /// Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.
        /// </summary>
        /// <param name="custom">Array of parameters that are host application specific.</param>
        /// <seealso cref="IDTExtensibility2"/>
        public void OnBeginShutdown(ref Array custom)
        {
        }

        /// <summary>
        /// Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.
        /// </summary>
        /// <param name="custom">Array of parameters that are host application specific.</param>
        /// <seealso cref="IDTExtensibility2"/>
        public void OnStartupComplete(ref Array custom)
        {
            this.eventsOnDocs = this.events.get_DocumentEvents();
            this.eventsOnProjectItems = this.events.ProjectItemsEvents;
            this.eventsOnSolution = this.events.SolutionEvents;
            this.eventsOnBuild = this.events.BuildEvents;
            this.eventsOnCommand = this.events.CommandEvents;

            this.eventsOnCommand.BeforeExecute += new _dispCommandEvents_BeforeExecuteEventHandler(this.CommandEvents_BeforeExecute);
            this.eventsOnSolution.Opened += new _dispSolutionEvents_OpenedEventHandler(this.SolutionEvents_Opened);
            this.eventsOnSolution.ProjectRemoved += new _dispSolutionEvents_ProjectRemovedEventHandler(this.SolutionEvents_ProjectRemoved);
            this.eventsOnSolution.AfterClosing += new _dispSolutionEvents_AfterClosingEventHandler(this.EventsOnSolution_AfterClosing);
            this.eventsOnProjectItems.ItemRenamed += new _dispProjectItemsEvents_ItemRenamedEventHandler(this.ProjectItemsEvents_ItemRenamed);
            this.eventsOnProjectItems.ItemAdded += new _dispProjectItemsEvents_ItemAddedEventHandler(this.ProjectItemsEvents_ItemAdded);
            this.eventsOnProjectItems.ItemRemoved += new _dispProjectItemsEvents_ItemRemovedEventHandler(this.ProjectItemsEvents_ItemRemoved);
            this.eventsOnDocs.DocumentSaved += new _dispDocumentEvents_DocumentSavedEventHandler(this.DocumentEvents_DocumentSaved);

            this.tasks = new TaskList(this.app);
            this.LoadActions();

            try
            {
                this.TreatLessAsCss(false);
            }
            catch (Exception ex)
            {
                this.OutputWindowWriteText("Error in TreatLessAsCss: " + ex.ToString());
            }

            // ensures the output window is lazy loaded so the multiple threads don't compete for and end up creating several
            this.OutputWindowWriteText("Ready");
        }
        #endregion

        #region Event Handlers
        static string[] buildCommands = new[] { "Build.BuildSelection", "Build.BuildSolution", "ClassViewContextMenus.ClassViewProject.Build" };

        void CommandEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            EnvDTE.Command objCommand = default(EnvDTE.Command);
            string sCommandName = null;

            try
            {
                objCommand = this.app.Commands.Item(Guid, ID);
            }
            catch (System.ArgumentException)
            {
            }
           

            if (objCommand != null)
            {
                sCommandName = objCommand.Name;

                if (Settings.T4RunAsBuild)
                {
                    if (buildCommands.Contains(sCommandName))
                    {
                        if (this.tasks != null) this.tasks.RemoveAll();
                        Engines.T4Engine.RunT4Template(this.app, Settings.T4RunAsBuildTemplate);
                    }
                }
            }
        }

        void SolutionEvents_Opened()
        {
            try
            {
                foreach (Project project in this.app.Solution.Projects)
                {
                    var ProjectItems = project.ProjectItems.ProcessFolderProjectItemsRecursively();
                    if (ProjectItems != null)
                    {
                        var configs = ProjectItems
                            .Where(x => ConfigEngine.Handles(x.Name) > 0);

                        foreach (ProjectItem config in configs)
                        {
                            EngineManager.Enqueue(config);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.OutputWindowWriteText(e.ToString());
            }
        }

        void ProjectItemsEvents_ItemAdded(ProjectItem projectItem)
        {
            try
            {
                EngineManager.Enqueue(projectItem);
            }
            catch (Exception e)
            {
                this.OutputWindowWriteText(e.ToString());
            }
        }

        void ProjectItemsEvents_ItemRenamed(ProjectItem projectItem, string oldFileName)
        {
            if (EngineManager.IsTransformed(projectItem.get_FileNames(1)))
            {
                // Now a chirp file
                this.ProjectItemsEvents_ItemAdded(projectItem);
            }
            else if (EngineManager.IsTransformed(oldFileName))
            {
                try
                {
                    VSProjectItemManager.DeleteAllItems(projectItem.ProjectItems);
                    this.tasks.Remove(oldFileName);
                }
                catch (Exception e)
                {
                    this.OutputWindowWriteText("Exception was thrown when trying to rename file.\n" + e.ToString());
                }
            }
        }

        void ProjectItemsEvents_ItemRemoved(ProjectItem projectItem)
        {
            string fileName = projectItem.get_FileNames(1);
            this.tasks.Remove(fileName);

            if (T4Engine.Handles(fileName) > 0)
            {
                this.T4Engine.Run(fileName, projectItem);
            }
        }

        void DocumentEvents_DocumentSaved(Document document)
        {
            try
            {
                ProjectItem item = document.ProjectItem;
                this.ProjectItemsEvents_ItemAdded(item);
            }
            catch (Exception e)
            {
                this.OutputWindowWriteText(e.ToString());
            }
        }

        #endregion

        #region Less

        public void TreatLessAsCss(bool force)
        {
            try
            {
                string extGuid = "{A764E898-518D-11d2-9A89-00C04F79EFC3}";
                string extPath = @"Software\Microsoft\VisualStudio\10.0_Config\Languages\File Extensions\.less";
                string editorGuid = "{A764E89A-518D-11d2-9A89-00C04F79EFC3}";
                string editorPath = string.Format(@"Software\Microsoft\VisualStudio\10.0_Config\Editors\{0}\Extensions", editorGuid);
                var user = Microsoft.Win32.Registry.CurrentUser;

                using (var extKey = user.OpenSubKey(extPath, true) ?? user.CreateSubKey(extPath))
                using (var editorKey = user.OpenSubKey(editorPath, true) ?? user.CreateSubKey(editorPath))
                {
                    if (force || string.IsNullOrEmpty(extKey.GetValue(string.Empty) as string) || (editorKey.GetValue("less", 0) as int? ?? 0) == 0)
                    {
                        extKey.SetValue(string.Empty, extGuid);
                        editorKey.SetValue("less", 0x28, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception e)
            {
                this.OutputWindowWriteText(e.ToString());
            }
        }
        #endregion

        #region "output login"
        public OutputWindowPane OutputWindowPane
        {
            get
            {
                if (this.lazyOutputWindowPane == null)
                {
                    this.lazyOutputWindowPane = this.GetOutputWindowPane("Chirpy");
                }

                return this.lazyOutputWindowPane;
            }
        }

        private OutputWindowPane GetOutputWindowPane(string name)
        {
            OutputWindow ow = this.app.ToolWindows.OutputWindow;
            OutputWindowPane owP;

            owP = ow.OutputWindowPanes.Cast<OutputWindowPane>().FirstOrDefault(x => x.Name == name);
            if (owP == null)
            {
                owP = ow.OutputWindowPanes.Add(name);
            }

            owP.Activate();
            return owP;
        }

        private void OutputWindowWriteText(string messageText)
        {
            try
            {
                this.OutputWindowPane.OutputString(messageText + System.Environment.NewLine);
            }
            catch (Exception eError)
            {
                MessageBox.Show(eError.ToString());
            }
        }
        #endregion
    }
}