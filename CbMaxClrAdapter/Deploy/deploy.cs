using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CbMaxClrAdapter.Deploy
{
   public class CPackage
   {
      private CPackage()
      {
         var aAssemblyDir = new FileInfo(typeof(CPackage).Assembly.Location).Directory;
         this.SourceDirectory = aAssemblyDir;
      }

      public DirectoryInfo SourceDirectory { get; set; }

      private readonly List<Tuple<FileInfo, Func<CPackage, FileInfo>>> FileInfos = new List<Tuple<FileInfo, Func<CPackage, FileInfo>>>();
      public FileInfo ExecuteableFile { get; set; }

      public void AddFile(FileInfo aSourceFileInfo, Func<CPackage, FileInfo> aGetTargetFile, bool aIsExecuteable = false)
      {
         if(!aSourceFileInfo.Exists)
         {
            throw new Exception("File missing: " + aSourceFileInfo.FullName);
         }
         else
         {
            this.FileInfos.Add(new Tuple<FileInfo, Func<CPackage, FileInfo>>(aSourceFileInfo, aGetTargetFile));
            if(aIsExecuteable)
            {
               this.ExecuteableFile = aGetTargetFile(this);
            }
         }
      }
      public void AddExternal(string aShortFileName, string aMaxDirSubFolder, bool aIsExecuteable = false)
      {
         var aFileInfo = new FileInfo(Path.Combine(this.SourceDirectory.FullName, aShortFileName));
         this.AddFile(aFileInfo, aPackage=>new FileInfo(Path.Combine(aPackage.MaxDirectory.FullName, aMaxDirSubFolder, aShortFileName)), aIsExecuteable);
      }

      private DirectoryInfo MaxDirectory;

      public static void Install(Action<CPackage> aSetupPackage)
      {
         var aTitle = "CbMaxClrAdapter";
         try
         {
            if (MessageBox.Show("This installs the max externals needed for the max standalone device respectively for the ableton live device.", aTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
            {
               var aMaxDir = new DirectoryInfo(@"C:\Program Files\Cycling '74\Max 8");
               var aPackage = new CPackage();
               var aFolderBrowserDialog = new FolderBrowserDialog();
               aFolderBrowserDialog.Description = "Select Max program installation directory.";
               aFolderBrowserDialog.RootFolder = Environment.SpecialFolder.ProgramFiles;
               aFolderBrowserDialog.SelectedPath = aMaxDir.Exists || true ? aMaxDir.FullName : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
               aFolderBrowserDialog.ShowNewFolderButton = false;
               if (aFolderBrowserDialog.ShowDialog() == DialogResult.OK)
               {
                  var aMaxDirectory = new DirectoryInfo(aFolderBrowserDialog.SelectedPath);
                  aPackage.MaxDirectory = aMaxDirectory;
                  aSetupPackage(aPackage);
                  foreach (var aFile in aPackage.FileInfos)
                  {
                     var aSourceFile = aFile.Item1;
                     var aTargetFile = aFile.Item2(aPackage);
                     if (aSourceFile.FullName.ToLower() != aTargetFile.FullName.ToLower())
                     {
                        aTargetFile.Directory.Create();
                        aSourceFile.CopyTo(aTargetFile.FullName, true);
                     }
                  }

                  MessageBox.Show("Successfullx installed.", aTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                  if (aPackage.ExecuteableFile is object)
                  {
                     System.Diagnostics.Process.Start(aPackage.ExecuteableFile.FullName);
                  }
               }
            }
         }
         catch(UnauthorizedAccessException aExc)
         {
            MessageBox.Show(aExc.Message + " Maybe you should start the application with administrator rights. (Right click, start as admin)", aTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         catch (Exception aExc)
         {
            MessageBox.Show(aExc.Message, aTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }


   }
}
