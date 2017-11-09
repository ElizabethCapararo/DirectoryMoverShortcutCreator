namespace Models
{
    public class ItemModel
    {
        public string FolderName { get; set; }
        public string FolderPath { get; set; }
        //public double SizeMB { get; set; }

        public ItemModel() { }
        public ItemModel(string _folder, string _folderPath) //, double _fileSize
        {
            this.FolderName = _folder;
            this.FolderPath = _folderPath;
            //this.SizeMB = _fileSize;
        }
    }
}
