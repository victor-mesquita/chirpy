﻿
using System;
using EnvDTE;
using EnvDTE80;
namespace Zippy.Chirp.Engines {
    public class T4Engine : ActionEngine {
        const string ControllerCSFile = ".cs";
        const string ControllerVBFile = ".vb";
        const string MVCViewFile = ".aspx";
        const string MVCPartialViewFile = ".ascx";
        const string RazorVBView = ".vbhtml";
        const string RazorCSView = ".cshtml";

        const string MVCT4TemplateName = "T4MVC.tt";

        bool IsMVCStandardControllerFile(string fileName) {
            return (fileName.EndsWith(ControllerCSFile, StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(ControllerVBFile, StringComparison.OrdinalIgnoreCase)) &&
                                        fileName.Contains("Controller");
        }

        bool IsMVCStandardViewScriptOrContentFile(string fileName) {
            return ((
                fileName.EndsWith(MVCViewFile, StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(MVCPartialViewFile, StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(RazorCSView, StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(RazorVBView, StringComparison.OrdinalIgnoreCase)
                ) && fileName.Contains("Views")) || fileName.Contains("Scripts") || fileName.Contains("Content");
        }

        public override int Handles(string filename) {
            return Settings.SmartRunT4MVC
                && (IsMVCStandardViewScriptOrContentFile(filename) || IsMVCStandardControllerFile(filename)) ? 1 : 0;
        }

        System.Threading.Timer tmr;
        public override void Run(string fullFileName, ProjectItem projectItem) {
            if (tmr != null) tmr.Dispose();
            tmr = new System.Threading.Timer((threadState) => RunT4Template(_Chirp.app, MVCT4TemplateName), null, 1000, System.Threading.Timeout.Infinite);
        }

        public static void RunT4Template(DTE2 app, string t4TemplateList) {
            try {

                string[] T4List = t4TemplateList.Split(new char[] { ',' });
                foreach (string t4Template in T4List) {
                    ProjectItem projectItem = app.Solution.FindProjectItem(t4Template.Trim());

                    if (projectItem != null) {
                        if (!projectItem.IsOpen)
                            projectItem.Open();
                        projectItem.Save();
                    }
                }
            } catch (Exception ex) {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }
    }


}
