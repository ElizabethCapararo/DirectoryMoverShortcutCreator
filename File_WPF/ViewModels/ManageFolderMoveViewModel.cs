using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using Models;
using Prism.Commands;
using ViewModels.Annotations;
using File = System.IO.File;

namespace ViewModels
{
    public class ManageFolderMoveViewModel :INotifyPropertyChanged
    {
        #region DelegateCommands
        public DelegateCommand BtnRefreshDelegateCommand { get; set; }
        public DelegateCommand<object> OpenFolderPickerDelegateCommand { get; set; }
        public DelegateCommand BtnRunCreateShortcutsDelegateCommand { get; set; }
        public DelegateCommand BtnRunReplaceFoldersplacWithShortcutsDelegateCommand { get; set; }
        #endregion

        #region Lists
        /// <summary>
        /// Holds the log of functions within the system for user viewing (live)
        /// </summary>
        public ObservableCollection<Event> Log
        {
            get => _log;
            set {
                _log = value;
                OnPropertyChanged(nameof(Log));
            }
        }
        /// <summary>
        /// All Folders collection, linked to DataGrid in View & holds ItemModel objects
        /// </summary>
        public ObservableCollection<ItemModel> AllFolders{
            get => _allFolders;
            set{
                _allFolders = value;
                OnPropertyChanged(nameof(AllFolders)); //property changed logic
            } 
        }
        #endregion

        #region Observed Variables
        /// <summary>
        /// Dictates if the main window is enabled, i.e. NOT processing
        /// </summary>
        public bool MainWindoWEnabled
        {
            get => _mainWindowEnabled;
            set
            {
                _mainWindowEnabled = value;
                OnPropertyChanged(nameof(MainWindoWEnabled));
            }
        }
        /// <summary>
        /// Dictates if the processing popup is visible, i.e. IS processing
        /// </summary>
        public bool Processing
        {
            get => _processing;
            set
            {
                _processing = value;
                OnPropertyChanged(nameof(Processing));
            }
        }
        /// <summary>
        /// Specifies whether to / from locations have been set, thus run button is enabled
        /// </summary>
        public bool BtnRunEnabled
        {
            get => _btnRunEnabled;
            set
            {
                _btnRunEnabled = value;
                OnPropertyChanged(nameof(BtnRunEnabled));
            }
        }
        /// <summary>
        /// Observable string used within TextBox of View, holds folder path to migrate from
        /// </summary>
        public string FromFolderPath{
            get => _fromFolderPath;
            set{
                _fromFolderPath = value;
                OnPropertyChanged(nameof(FromFolderPath));
                if (ToFolderPath != null && !ToFolderPath.Equals("")) BtnRunEnabled = true;
                else BtnRunEnabled = false;
                GetAllFolders();;
            }
        }
        /// <summary>
        /// Observable string used within TextBox of view, holds folder path to migrate to
        /// </summary>
        public string ToFolderPath {
            get => _toFolderPath;
            set{
                _toFolderPath = value;
                OnPropertyChanged(nameof(ToFolderPath));

                if (FromFolderPath != null && !FromFolderPath.Equals("")) BtnRunEnabled = true;
                else BtnRunEnabled = false;
            }
        }
        #endregion

        #region Supplementary Variables / Lists
        private ObservableCollection<Event> _log;
        private bool _btnRunEnabled;
        private bool _mainWindowEnabled;
        private bool _processing;
        private string _fromFolderPath;
        private string _toFolderPath;
        private ObservableCollection<ItemModel> _allFolders;
        #endregion

        #region Constructor
        public ManageFolderMoveViewModel()
        {
          /* Button's Enabled Status */
            BtnRunEnabled = false;

            /* Window's Enabled Status */
            Processing = false;
            MainWindoWEnabled = true;

            /* Log */
            Log = new ObservableCollection<Event>();
            Log.Add(new Event(Event.ErrorTypes.Info, "System started"));

            /* Folder's Selection / list, etc */
            AllFolders = new ObservableCollection<ItemModel>();
            FromFolderPath = "";
            ToFolderPath = "";

            /* Delegate Command Instantiation */
            OpenFolderPickerDelegateCommand = new DelegateCommand<object>(OpenFolderPicker);
            BtnRunCreateShortcutsDelegateCommand = new DelegateCommand(RunCreateShortcuts);
            BtnRunReplaceFoldersplacWithShortcutsDelegateCommand = new DelegateCommand(RunMoveReplaceWithShortcuts);
            BtnRefreshDelegateCommand = new DelegateCommand(Refresh);
        }
        #endregion

        #region DelegateCommands Functions
        /// <summary>
        /// Refreshes the visiblle list of folders within the FROM i.e. Destination directory
        /// </summary>
        private void Refresh()
        {
            GetAllFolders();
        }
        /// <summary>
        /// Replaces all folders in a directory with shortcuts, moves the original items to a different folder
        /// </summary>
        private void RunMoveReplaceWithShortcuts()
        {
            ToggleProcessing(); //processing ON
            MoveDirectory(FromFolderPath, ToFolderPath);
            CreateDirectory(FromFolderPath);

            Log.Add(new Event(Event.ErrorTypes.Info, "Creating shortcuts for " + AllFolders.Count + " items"));

            foreach (var f in AllFolders)
            {
                CreateShortcut(f.FolderName, FromFolderPath, ToFolderPath);
            }
            ToggleProcessing(); //processing OFF
        }
        /// <summary>
        /// Creates shortcuts for every folder in a directory
        /// </summary>
        private void RunCreateShortcuts()
        {
            ToggleProcessing(); //processing ON
            Log.Add(new Event(Event.ErrorTypes.Info, "Creating shortcuts for " + AllFolders.Count + " items"));

            foreach (var f in AllFolders)
            {
                CreateShortcut(f.FolderName, ToFolderPath, FromFolderPath);
            }
            ToggleProcessing(); //processing OFF
        }
        /// <summary>
        /// Opens a folder picker so that the user may select a TO and FROM folder i.e. destination / source
        /// </summary>
        /// <param name="o"></param>
        private void OpenFolderPicker(object o)
        {
            if(o == null) return;

            string _folderPickerType = o.ToString();
            string _selectedPath = "";

            if (_folderPickerType.Equals("DestinationFolder")) _selectedPath = ToFolderPath;
            else if (_folderPickerType.Equals("SourceFolder")) _selectedPath = FromFolderPath;

            FolderBrowserDialog _folderBrowserDialog = new FolderBrowserDialog
            {
                SelectedPath = _selectedPath
            };

            if(_folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                if (_folderPickerType.Equals("DestinationFolder"))
                {
                    ToFolderPath = _folderBrowserDialog.SelectedPath;
                    Log.Add(new Event(Event.ErrorTypes.Info, "Directory set " + ToFolderPath));
                }
                else if (_folderPickerType.Equals("SourceFolder"))
                {
                    FromFolderPath = _folderBrowserDialog.SelectedPath;
                    Log.Add(new Event(Event.ErrorTypes.Info, "Directory set " + FromFolderPath));
                }
            }
        }
        #endregion

        #region Functions
        /// <summary>
        /// Creates a new directory
        /// </summary>
        /// <param name="_fromFolderPath"></param>
        private void CreateDirectory(string _fromFolderPath)
        {
            Log.Add(new Event(Event.ErrorTypes.Info, "Recreating Directory " + FromFolderPath));
            Directory.CreateDirectory(_fromFolderPath);
        }
        /// <summary>
        /// Moves an entire directory
        /// </summary>
        /// <param name="sourcedirectory"></param>
        /// <param name="destinationdirectory"></param>
        private void MoveDirectory(string sourcedirectory, string destinationdirectory)
        {
            try
            {
                Log.Add(new Event(Event.ErrorTypes.Info, "Moving Directory from " + FromFolderPath + " to " + ToFolderPath));
                
                Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(sourcedirectory, destinationdirectory);
            }
            catch (Exception e)
            {
                Log.Add(new Event(Event.ErrorTypes.Error, "Unable to move Directory from " + FromFolderPath + " to " + ToFolderPath + "\n" + e));
                Console.WriteLine(e);
            }
        }
        /// <summary>
        /// Creates a new shortcut
        /// </summary>
        /// <param name="shortcutName"></param>
        /// <param name="shortcutPath"></param>
        /// <param name="targetFileLocation"></param>
        private void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation)
        {
            try
            {
                var shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut) shell.CreateShortcut(shortcutLocation);

                shortcut.Description = "Shortcut to " + shortcutName;
                shortcut.TargetPath = Path.Combine(targetFileLocation, shortcutName);
                shortcut.Save();
            }
            catch (Exception e)
            {
                Log.Add(new Event(Event.ErrorTypes.Error, "Unable to create Shortcut " + shortcutName + " \n" + e));
            }
        }
        /// <summary>
        /// Clears the existing list
        /// </summary>
        private void ClearMappings() { AllFolders = new ObservableCollection<ItemModel>(); }
       
        /// <summary>
        /// Gets all folders from a directory, and populates the AllFolders list with itemModel objects
        /// </summary>
        private void GetAllFolders()
        {
            ClearMappings();

            string _folderPath = FromFolderPath; //set directory

            try
            {
                foreach (var d in Directory.GetDirectories(_folderPath))
                {
                    File.SetAttributes(d, FileAttributes.Normal);

                    ItemModel m = new ItemModel
                    {
                        FolderPath = d,
                        FolderName = ExtractFolderName(d), //gets a folder name from path
                        //SizeMB = Math.Round(GetDirectorySize(d) / 1048576, 2) //1024 * 1024 gets MB Math.Round(_length / 1048576, 2)
                    };

                    AllFolders.Add(m);
                }
            }
            catch (Exception) {  }
        }
        /*
        private double size = 0;
        private double GetDirectorySize(string _directory)
        {
            try
            {
                foreach (var d in Directory.GetDirectories(_directory))
                {
                    GetDirectorySize(d);
                }

                foreach (var f in new DirectoryInfo(_directory).GetFiles())
                {
                    size += f.Length;
                }
            }
            catch (Exception) { }

            return size;
        }
        */
        /// <summary>
        /// Extracts a folder name from the given folder path (used for display purposes / readability)
        /// </summary>
        /// <param name="_folderPath"></param>
        /// <returns></returns>
        private string ExtractFolderName(string _folderPath)
        {
            return _folderPath.Split('\\').Last(); //LINQ -- gets 2nd half of string after the last appearance of '\'
        }
        /// <summary>
        /// Toggles views
        /// </summary>
        private void ToggleProcessing()
        {
            if (Processing)
            {
                Processing = false;
                MainWindoWEnabled = true;
            }
            else
            {
                MainWindoWEnabled = false;
                Processing = true;
            }
        }
        #endregion

        #region Property Changed Logic
        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
