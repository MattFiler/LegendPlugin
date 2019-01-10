﻿/*
 * 
 * Created by Matt Filer
 * www.mattfiler.co.uk
 * 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Alien_Isolation_Mod_Tools;

namespace PackagingTool
{
    public partial class BehaviourPacker : Form
    {
        Directories AlienDirectories = new Directories();

        //Settings
        string openFolderOnExport = "1";
        string openGameOnImport = "0";
        string showMessageBoxes = "1";

        /* ONLOAD */
        public BehaviourPacker()
        {
            InitializeComponent();
            
            //Get settings values
            getSettings();

            //Bring to front
            this.WindowState = FormWindowState.Minimized;
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        /* UNPACK */
        private void unpackButton_Click(object sender, EventArgs e)
        {
            if (!File.Exists(AlienDirectories.ToolTreeDirectory() + "alien_all_search_variants.xml")) {
                /* STARTING */
                unpackButton.Enabled = false;
                repackButton.Enabled = false;
                resetTrees.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;

                /* COPY _BINARY_BEHAVIOUR TO WORKING DIRECTORY */
                File.Copy(AlienDirectories.GameDirectoryRoot() + @"\DATA\BINARY_BEHAVIOR\_DIRECTORY_CONTENTS.BML", AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.BML");

                /* CONVERT _BINARY_BEHAVIOUR TO XML */
                new AlienConverter(AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.BML", AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.xml").Run();

                /* EXTRACT XML TO SEPARATE FILES */
                string directoryContentsXML = File.ReadAllText(AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.xml"); //Get contents from newly converted _DIRECTORY_CONTENTS
                string fileHeader = "<?xml version='1.0' encoding='utf-8'?>\n<Behavior>"; //Premade file header
                int count = 0;

                foreach (string currentFile in Regex.Split(directoryContentsXML, "<File name="))
                {
                    count += 1;
                    if (count != 1)
                    {
                        string[] extractedContents = Regex.Split(currentFile, "<Behavior>"); //Split filename and contents
                        string[] extractedContentsMain = Regex.Split(extractedContents[1], "</File>"); //Split contents and footer
                        string[] fileContents = { fileHeader, extractedContentsMain[0] }; //Write preset header and newly grabbed contents
                        string fileName = "";
                        if (File.Exists(AlienDirectories.GameDirectoryRoot() + @"\DATA\BINARY_BEHAVIOR\gameismodded.txt") || //legacy
                            File.Exists(AlienDirectories.GameDirectoryRoot() + @"\DATA\BINARY_BEHAVIOR\packagingtool_hasmodded.ayz"))
                        {
                            fileName = extractedContents[0].Substring(1, extractedContents[0].Length - 9); //Grab filename
                        }
                        else
                        {
                            fileName = extractedContents[0].Substring(1, extractedContents[0].Length - 11); //Grab filename UNMODDED FILE
                        }

                        File.WriteAllLines(AlienDirectories.ToolTreeDirectory() + fileName + ".xml", fileContents); //Write new file
                    }
                }

                /* DELETE EXCESS FILES */
                File.Delete(AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.BML");
                File.Delete(AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.xml");

                /* DONE */
                Cursor.Current = Cursors.Default;
                unpackButton.Enabled = true;
                repackButton.Enabled = true;
                resetTrees.Enabled = true;
            }

            /* OPEN BRAINIAC */
            ProcessStartInfo brainiacProcess = new ProcessStartInfo();
            brainiacProcess.WorkingDirectory = AlienDirectories.BrainiacDirectoryRoot();
            brainiacProcess.FileName = "Brainiac Designer.exe";
            Process myProcess = Process.Start(brainiacProcess);
        }

        /* REPACK */
        private void repackButton_Click(object sender, EventArgs e)
        {
            if (File.Exists(AlienDirectories.ToolTreeDirectory() + "alien_all_search_variants.xml"))
            {
                /* STARTING */
                unpackButton.Enabled = false;
                repackButton.Enabled = false;
                resetTrees.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;

                /* WRITE NEW _DIRECTORY_CONTENTS XML AND DELETE FILES */
                string compiledBinaryBehaviourContents = "<?xml version=\"1.0\" encoding=\"utf-8\"?><DIR>"; //Start file

                DirectoryInfo workingDirectoryInfo = new DirectoryInfo(AlienDirectories.ToolTreeDirectory()); //Get all files in directory
                foreach (FileInfo currentFile in workingDirectoryInfo.GetFiles())
                {
                    string fileContents = File.ReadAllText(currentFile.FullName); //Current file contents
                    string fileName = currentFile.Name; //Current file name
                    string customFileHeader = "<File name=\"" + fileName.Substring(0, fileName.Length - 3) + "bml\">"; //File header
                    string customFileFooter = "</File>"; //File footer

                    compiledBinaryBehaviourContents += customFileHeader + fileContents.Substring(38) + customFileFooter; //Add to file string
                }

                compiledBinaryBehaviourContents += "</DIR>"; //Finish off file string

                string[] compiledContentsAsArray = { compiledBinaryBehaviourContents };
                File.WriteAllLines(AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.xml", compiledContentsAsArray); //Write new file

                /* CONVERT _BINARY_BEHAVIOUR TO BML */
                new AlienConverter(AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.xml", AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.bml").Run();

                /* COPY _BINARY_BEHAVIOUR TO GAME AND DELETE FILES */
                File.Delete(AlienDirectories.GameDirectoryRoot() + @"\DATA\BINARY_BEHAVIOR\_DIRECTORY_CONTENTS.BML");
                File.Copy(AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.bml", AlienDirectories.GameDirectoryRoot() + @"\DATA\BINARY_BEHAVIOR\_DIRECTORY_CONTENTS.BML");
                string[] moddedGameText = { "DO NOT DELETE THIS FILE" };
                File.WriteAllLines(AlienDirectories.GameDirectoryRoot() + @"\DATA\BINARY_BEHAVIOR\packagingtool_hasmodded.ayz", moddedGameText); //Write modded game text
                File.Delete(AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.bml");
                File.Delete(AlienDirectories.ToolTreeDirectory() + "_DIRECTORY_CONTENTS.xml");

                /* DONE */
                Cursor.Current = Cursors.Default;
                getSettings();
                if (showMessageBoxes == "1")
                {
                    MessageBox.Show("Modifications have been imported.");
                }
                unpackButton.Enabled = true;
                repackButton.Enabled = true;
                resetTrees.Enabled = true;
                getSettings(); //Check for settings
                if (openGameOnImport == "1")
                {
                    Process.Start(AlienDirectories.GameDirectoryRoot() + @"\AI.exe"); //Open output folder if requested
                }
            }
            else
            {
                MessageBox.Show("No modifications have been made! Nothing to import.");
            }
        }

        /* RESET */
        private void resetTrees_Click(object sender, EventArgs e)
        {
            /* STARTING */
            unpackButton.Enabled = false;
            repackButton.Enabled = false;
            resetTrees.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;

            /* COPY ORIGINAL FILE FROM PROJECT MEMORY */
            File.WriteAllBytes(AlienDirectories.GameDirectoryRoot() + @"\DATA\BINARY_BEHAVIOR\_DIRECTORY_CONTENTS.BML", Alien_Isolation_Mod_Tools.Properties.Resources._DIRECTORY_CONTENTS);
            File.Delete(AlienDirectories.GameDirectoryRoot() + @"\DATA\BINARY_BEHAVIOR\gameismodded.txt"); //legacy
            File.Delete(AlienDirectories.GameDirectoryRoot() + @"\DATA\BINARY_BEHAVIOR\packagingtool_hasmodded.ayz");

            /* DELETE ALL EXTRACTED TREES */
            DirectoryInfo workingDirectoryInfo = new DirectoryInfo(AlienDirectories.ToolTreeDirectory()); //Get all files in directory
            foreach (FileInfo currentFile in workingDirectoryInfo.GetFiles())
            {
                currentFile.Delete();
            }

            /* DONE */
            Cursor.Current = Cursors.Default;
            MessageBox.Show("Behaviour trees have been reset to defaults.\nIf you have the editor open, close it and re-open through this window.");
            unpackButton.Enabled = true;
            repackButton.Enabled = true;
            resetTrees.Enabled = true;
        }

        /* OPEN OPTIONS */
        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BehaviourPackerSettings settingsForm = new BehaviourPackerSettings();
            settingsForm.Show();
        }

        /* OPEN ATTRIBUTE EDITOR */
        private void attributeEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //CharEd attributeForm = new CharEd();
            //attributeForm.Show();
        }

        /* GET CURRENT SETTINGS */
        private void getSettings()
        {
            int loopCount = 0;
            foreach (var line in File.ReadLines(Directory.GetCurrentDirectory() + @"\modtools_settings.ayz"))
            {
                switch (line)
                {
                    case "0":
                        if (loopCount == 0)
                        {
                            openFolderOnExport = "0";
                        }
                        if (loopCount == 1)
                        {
                            openGameOnImport = "0";
                        }
                        if (loopCount == 2)
                        {
                            showMessageBoxes = "0";
                        }
                        break;
                    case "1":
                        if (loopCount == 0)
                        {
                            openFolderOnExport = "1";
                        }
                        if (loopCount == 1)
                        {
                            openGameOnImport = "1";
                        }
                        if (loopCount == 2)
                        {
                            showMessageBoxes = "1";
                        }
                        break;
                    default:
                        break;
                }
                loopCount += 1;
            }
        }

        /* FORM LOAD */
        private void Form1_Load(object sender, EventArgs e)
        {
            //Currently unused.
        }
    }
}
