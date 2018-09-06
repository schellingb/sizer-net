/*
  Sizer.Net
  Written by Bernhard Schelling
  https://github.com/schellingb/sizer-net/

  This is free and unencumbered software released into the public domain.

  Anyone is free to copy, modify, publish, use, compile, sell, or
  distribute this software, either in source code form or as a compiled
  binary, for any purpose, commercial or non-commercial, and by any
  means.

  In jurisdictions that recognize copyright laws, the author or authors
  of this software dedicate any and all copyright interest in the
  software to the public domain. We make this dedication for the benefit
  of the public at large and to the detriment of our heirs and
  successors. We intend this dedication to be an overt act of
  relinquishment in perpetuity of all present and future rights to this
  software under copyright law.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
  IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
  OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
  ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
  OTHER DEALINGS IN THE SOFTWARE.

  For more information, please refer to <http://unlicense.org/>
*/

using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("Sizer.Net")]
[assembly: AssemblyProduct("sizer-net")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: System.Runtime.InteropServices.ComVisible(false)]

static class SizerNet
{
    static Form f;
    static TreeView tv;
    static int TreeViewScrollX = 0;
    static string AssemblyPath;
    static long AssemblySize;
    static string[] DependencyDirs, IgnoredDependencies;

    [STAThread] static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        f = new Form();
        f.KeyPreview = true;
        f.KeyUp += (object sender, KeyEventArgs e) => { if (e.KeyCode == Keys.Escape) ((Form)sender).Close(); };
        f.Text = "Sizer.Net";
        f.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        f.ClientSize = new Size(800, 700);

        tv = new TreeView();
        tv.Location = new Point(13, 13);
        tv.Size = new Size(f.ClientSize.Width - 13 - 13, f.ClientSize.Height - 13 - 23 - 13 - 13);
        tv.DrawMode = TreeViewDrawMode.OwnerDrawText;
        tv.Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
        tv.Resize += (object sender, EventArgs e) => { tv.Invalidate(); };
        tv.DrawNode += OnTreeDrawNode;
        f.Controls.Add(tv);

        Button btnLoad = new Button();
        btnLoad.Location = new Point(13, f.ClientSize.Height - 13 - 23);
        btnLoad.Size = new Size(500 - 13, 23);
        btnLoad.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
        btnLoad.Text = "Load Assembly";
        btnLoad.Click += (object sender, EventArgs e) => { BrowseAssembly(); };
        f.Controls.Add(btnLoad);

        Button btnClose = new Button();
        btnClose.Location = new Point(513, f.ClientSize.Height - 13 - 23);
        btnClose.Size = new Size(f.ClientSize.Width - 513 - 13, 23);
        btnClose.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Right);
        btnClose.Text = "Close";
        btnClose.Click += (object sender, EventArgs e) => { f.Close(); };
        f.Controls.Add(btnClose);

        if (args.Length > 0)
        {
            if (new FileInfo(args[0]).Exists == false)
            {
                MessageBox.Show("Assembly not found: " + args[0], "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            f.Shown += (object sender, EventArgs e) => { LoadAssembly(args[0], true); };
        }
        else f.Shown += (object sender, EventArgs e) => { BrowseAssembly(true); };

        Application.Run(f);
    }

    static void BrowseAssembly(bool InitialLoad = false)
    {
        var ofd = new OpenFileDialog();
        ofd.ValidateNames = ofd.CheckFileExists = ofd.CheckPathExists = true;
        ofd.Filter = ".Net Assemblies (*.exe, *.dll)|*.exe;*.dll";
        if (ofd.ShowDialog() != DialogResult.OK) { if (InitialLoad) f.Close(); return; }
        ofd.Dispose();
        LoadAssembly(ofd.FileName, InitialLoad);
    }

    static void OnTreeDrawNode(object sender, DrawTreeNodeEventArgs e)
    {
        e.DrawDefault = true;
        if (tv.Nodes.Count == 0 || e.Bounds.Height == 0) return;
        float pct = (float)(long)e.Node.Tag / AssemblySize;
        int w = tv.ClientSize.Width / 4, x = tv.ClientSize.Width - w - 5, size = (int)(w * pct);
        e.Graphics.FillRectangle(Brushes.White,     x, e.Bounds.Top + 1, w    + 1, e.Bounds.Height - 2);
        e.Graphics.FillRectangle(Brushes.LightGray, x, e.Bounds.Top + 1, size + 1, e.Bounds.Height - 2);
        e.Graphics.DrawRectangle(Pens.DarkGray,     x, e.Bounds.Top + 1, w    + 1, e.Bounds.Height - 2);
        if  (true) //ShowInKilobytes
            e.Graphics.DrawString(((float)(long)e.Node.Tag/1024f).ToString("0.##") + " kb", tv.Font, Brushes.DarkSlateGray, x, e.Bounds.Top + 1);
        //else (ShowInBytes)
        //  e.Graphics.DrawString(((long)e.Node.Tag).ToString() + " b", tv.Font, Brushes.DarkSlateGray, x, e.Bounds.Top + 1);
        //else (ShowInPercent)
        //  e.Graphics.DrawString((pct*100).ToString("0.##") + "%", tv.Font, Brushes.DarkSlateGray, x, e.Bounds.Top + 1);
        e.Graphics.DrawLine((e.State & TreeNodeStates.Selected) != 0 ? SystemPens.Highlight : SystemPens.ControlLight, e.Bounds.Left + 5, e.Bounds.Top + e.Bounds.Height/2, x - 5, e.Bounds.Top + e.Bounds.Height/2);
        if (tv.Nodes[0].Bounds.X != TreeViewScrollX) { TreeViewScrollX = tv.Nodes[0].Bounds.X; tv.Invalidate(); }
    }

    //estimated numbers for byte size of overhead introduced by various things
    const int Overhead_Type            = 4+8*2;
    const int Overhead_Field           = 2+2*2;
    const int Overhead_Method          = 8+6*2;
    const int Overhead_LocalVariable   = 4+1*2;
    const int Overhead_Parameter       = 4+1*2;
    const int Overhead_InterfaceImpl   = 0+2*2;
    const int Overhead_Event           = 2+2*2;
    const int Overhead_Property        = 2+2*2;
    const int Overhead_CustomAttribute = 0+3*2;

    static void LoadAssembly(string InAssemblyPath, bool InitialLoad = false)
    {
        AssemblyPath = InAssemblyPath;
        try
        {
            tv.Nodes.Clear();
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveExternalAssembly;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveExternalAssembly;
            AssemblyPath = new FileInfo(AssemblyPath).FullName;
            DependencyDirs = new [] { Path.GetDirectoryName(AssemblyPath), Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) };
            IgnoredDependencies = new string[0];
            Assembly assembly = Assembly.LoadFile(AssemblyPath);
            bool IsReflectionOnly = false;
            AssemblySize = new FileInfo(assembly.Location).Length;
            if (AssemblyPath != assembly.Location && !FileContentsMatch(AssemblyPath, assembly.Location))
            {
                MessageBox.Show("Requested assembly:\n" + AssemblyPath + "\n\nAssembly loaded by system:\n" + assembly.Location + "\n\nA different assembly was loaded because an assembly with the same name exists in the global assembly cache.\n\nResorting to loading the assembly in 'reflection only' mode which disables dependency resolving which can make certain type evaluations impossible.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                assembly = Assembly.ReflectionOnlyLoadFrom(AssemblyPath);
                IsReflectionOnly = true;
            }

            BindingFlags all = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            BindingFlags statics = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            TreeNode nAssembly = new TreeNode(assembly.GetName().Name);
            nAssembly.Tag = 0L;

            TreeNode nResources = nAssembly.Nodes.Add("Resources");
            nResources.Tag = 0L;

            //Enumerate Win32 resources
            try
            {
                IntPtr AssemblyHandle = LoadLibraryEx(AssemblyPath, IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE);
                if (AssemblyHandle == IntPtr.Zero) throw new Exception();
                EnumResourceTypes(AssemblyHandle, new EnumResTypeProc((IntPtr hModule, IntPtr lpszType, IntPtr lParam) =>
                {
                    try { lpszType.ToInt32(); } catch (Exception) { return true; }
                    EnumResourceNames(hModule, lpszType.ToInt32(), new EnumResNameProc((IntPtr hModule2, IntPtr lpszType2, IntPtr lpzName, IntPtr lParam2) =>
                    {
                        ResType rt =  unchecked((ResType)(long)lpszType2);
                        IntPtr hResource = FindResource(hModule2, lpzName, lpszType2);
                        long Size = SizeofResource(hModule2, hResource);

                        string name = System.Runtime.InteropServices.Marshal.PtrToStringUni(lpzName);
                        TreeNode nResource = nResources.Nodes.Add("Resource: " + rt.ToString() + " " + (name == null ? "#" + lpzName.ToInt64() : name));
                        SetNodeTag(nResource, Size);

                        return true;
                    }), IntPtr.Zero);
                    return true;
                }), IntPtr.Zero);
                FreeLibrary(AssemblyHandle);
            }
            catch (Exception) { } //ignore Win32 resources

            //Enumerate manifest resources
            foreach (string mr in assembly.GetManifestResourceNames())
            {
                ResourceLocation rl = assembly.GetManifestResourceInfo(mr).ResourceLocation;
                if ((rl & ResourceLocation.Embedded) == 0 || (rl & ResourceLocation.ContainedInAnotherAssembly) != 0) continue;
                TreeNode nResource = nResources.Nodes.Add("Manifest Resource: " + mr);
                Stream mrs = assembly.GetManifestResourceStream(mr);
                SetNodeTag(nResource, mrs.Length);
                mrs.Dispose();
            }

            foreach (Module module in assembly.GetModules())
            {
                foreach (MethodInfo mi in module.GetMethods(all)) AddMethodNode(nAssembly, mi);

                int lenModuleFields = 0;
                foreach (FieldInfo fi in module.GetFields(all)) lenModuleFields += Overhead_Field + fi.Name.Length;
                if (lenModuleFields != 0) { TreeNode nModuleInfo = nAssembly.Nodes.Add(module.GetFields(all).Length.ToString() + " Fields in " + module.Name + " (Overhead)"); SetNodeTag(nModuleInfo, lenModuleFields);     }
            }

            Type[] AssemblyTypes;
            try { AssemblyTypes = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { AssemblyTypes = e.Types; }

            int UnresolvedTypes = 0;
            foreach (Type type in AssemblyTypes)
            {
                if (type == null) { UnresolvedTypes++; continue; }
                TreeNode nType = nAssembly;
                bool IsStaticArrayInitType = type.Name.Contains("StaticArrayInitTypeSize=");
                foreach (string NSPart in type.FullName.Split('.', '+'))
                {
                    if (nType.Nodes.ContainsKey(NSPart)) nType = nType.Nodes[NSPart];
                    else (nType = nType.Nodes.Add(NSPart, NSPart)).Tag = 0L;
                }

                int lenType = Overhead_Type + type.FullName.Length;
                try { foreach (Type it in type.GetInterfaces()) lenType += Overhead_InterfaceImpl; } catch { }
                #if DOTNET35
                try { foreach (object ca in type.GetCustomAttributes(false)) lenType += Overhead_CustomAttribute; } catch { }
                #else
                try { foreach (CustomAttributeData ad in type.GetCustomAttributesData()) lenType += Overhead_CustomAttribute; } catch { }
                #endif
                SetNodeTag(nType, lenType);

                foreach (FieldInfo fi in type.GetFields(statics))
                {
                    try
                    {
                        if (fi.FieldType.ContainsGenericParameters || fi.FieldType.IsGenericType) continue;
                        long fiSize = CalculateSize(IsReflectionOnly, fi.FieldType, fi);
                        if (fiSize > 0) SetNodeTag(nType.Nodes.Add("Static Field: " + fi.Name), fiSize);
                    }
                    catch (Exception) { }
                }

                int numTypeFields = 0, numTypeProperties = 0, numTypeEvents = 0, lenTypeFields = 0, lenTypeProperties = 0, lenTypeEvents = 0;
                foreach (FieldInfo    fi in type.GetFields(all))     { numTypeFields++;     lenTypeFields     += Overhead_Field    + fi.Name.Length;                         }
                foreach (PropertyInfo pi in type.GetProperties(all)) { numTypeProperties++; lenTypeProperties += Overhead_Property + (pi.Name == null ? 0 : pi.Name.Length); }
                foreach (EventInfo    ei in type.GetEvents(all))     { numTypeEvents++;     lenTypeEvents     += Overhead_Event    + ei.Name.Length;                         }
                if (lenTypeFields     != 0) SetNodeTag(nType.Nodes.Add(numTypeFields.ToString()     + " Fields (Overhead)"),     lenTypeFields);
                if (lenTypeProperties != 0) SetNodeTag(nType.Nodes.Add(numTypeProperties.ToString() + " Properties (Overhead)"), lenTypeProperties);
                if (lenTypeEvents     != 0) SetNodeTag(nType.Nodes.Add(numTypeEvents.ToString()     + " Events (Overhead)"),     lenTypeEvents);

                foreach (ConstructorInfo ci in type.GetConstructors(all)) AddMethodNode(nType, ci);
                foreach (MethodInfo      mi in type.GetMethods(all))      AddMethodNode(nType, mi);
            }

            SetNodeTag(nAssembly.Nodes.Add("Other Overhead"), AssemblySize - (long)nAssembly.Tag);
            SortByNodeByTag(nAssembly.Nodes);
            //FilterNodeByTag(nAssembly.Nodes, AssemblySize/100);
            nAssembly.Expand();
            tv.Nodes.Add(nAssembly);

            if (UnresolvedTypes != 0)
            {
                MessageBox.Show(UnresolvedTypes.ToString() + " types could not be evaluated due to missing dependency errors.\nThese are included in the 'Other Overhead' entry.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception e)
        {
            MessageBox.Show("Assembly loading error:\n\n" + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            if (InitialLoad) f.Close();
        }
    }
    
    static void AddMethodNode(TreeNode ParentNode, MethodBase mi)
    {
        TreeNode nMethod = ParentNode.Nodes.Add(mi.Name);
        int lenMi = Overhead_Method + mi.Name.Length;
        try { var mb = mi.GetMethodBody(); lenMi += mb.GetILAsByteArray().Length; foreach (LocalVariableInfo lvi in mb.LocalVariables) lenMi += Overhead_LocalVariable; } catch { }
        try { foreach (ParameterInfo pi in mi.GetParameters()) lenMi += 16 + (pi.Name == null ? 0 : pi.Name.Length); } catch { }
        #if DOTNET35
        try { foreach (object ca in mi.GetCustomAttributes(false)) lenMi += Overhead_CustomAttribute; } catch { }
        #else
        try { foreach (CustomAttributeData ad in mi.GetCustomAttributesData()) lenMi += Overhead_CustomAttribute; } catch { }
        #endif
        SetNodeTag(nMethod, lenMi);
    }

    static Assembly ResolveExternalAssembly(object sender, ResolveEventArgs args)
    {
        if (System.Array.IndexOf<string>(IgnoredDependencies, args.Name) >= 0) return null;

        string DllFileName = new AssemblyName(args.Name).Name + ".dll";
        foreach (string dir in DependencyDirs)
        {
            string TestAssemblyPath = Path.Combine(dir, DllFileName);
            if (File.Exists(TestAssemblyPath)) return Assembly.LoadFile(TestAssemblyPath);
        }

        var ofd = new OpenFileDialog();
        ofd.Title = "Find dependency: " + args.Name;
        ofd.ValidateNames = ofd.CheckFileExists = ofd.CheckPathExists = true;
        ofd.FileName = DllFileName;
        ofd.Filter = ".Net Dependencies (*.dll)|*.dll";
        if (ofd.ShowDialog() != DialogResult.OK)
        {
            System.Array.Resize<string>(ref IgnoredDependencies, IgnoredDependencies.Length + 1);
            IgnoredDependencies[IgnoredDependencies.Length - 1] = args.Name;
            return null;
        }
        System.Array.Resize<string>(ref DependencyDirs, DependencyDirs.Length + 1);
        DependencyDirs[DependencyDirs.Length - 1] = Path.GetDirectoryName(ofd.FileName);
        return Assembly.LoadFile(ofd.FileName);
    }

    static void SetNodeTag(TreeNode n, long amount)
    {
        n.Tag = amount;
        for (n = n.Parent; n != null; n = n.Parent) n.Tag = ((long)n.Tag) + amount;
    }

    static void SortByNodeByTag(TreeNodeCollection nc)
    {
        foreach (TreeNode n in nc) SortByNodeByTag(n.Nodes);
        TreeNode[] ns = new TreeNode[nc.Count];
        nc.CopyTo(ns, 0);
        Array.Sort<TreeNode>(ns, (TreeNode a, TreeNode b) => { return unchecked((int)((long)b.Tag - (long)a.Tag)); });
        nc.Clear();
        nc.AddRange(ns);
    }

    static void FilterNodeByTag(TreeNodeCollection nc, long Threshold)
    {
        long TotalRemoved = 0;
        int LastRemoved = -1;
        for (int i = 0; i != nc.Count; i++)
        {
            if ((long)nc[i].Tag < Threshold) { TotalRemoved += (long)nc[i].Tag; nc[i].Remove(); LastRemoved = i--; continue; }
            FilterNodeByTag(nc[i].Nodes, Threshold);
        }
        if (TotalRemoved != 0) {  nc.Add("... <Filtered> ...").Tag = TotalRemoved; }
    }

    static long CalculateSize(bool IsReflectionOnly, Type t, object FiOrValue = null)
    {
        if (t.IsArray)
        {
            System.Array a = (System.Array)(FiOrValue is FieldInfo ? (IsReflectionOnly ? ((FieldInfo)FiOrValue).GetRawConstantValue() : ((FieldInfo)FiOrValue).GetValue(null)) : FiOrValue);
            if (a == null || a.LongLength == 0) return 0;
            t = t.GetElementType();
            if (t.IsEnum) t = Enum.GetUnderlyingType(t);
            if (!t.ContainsGenericParameters && !t.IsGenericType && (t.IsValueType || t.IsPointer || t.IsLayoutSequential)) return a.LongLength * System.Runtime.InteropServices.Marshal.SizeOf(t);
            if (!t.IsArray && t != typeof(string)) return 0; //can't measure size
            long res = 0;
            foreach (object v in a) res += CalculateSize(IsReflectionOnly, t, v);
            return res;
        }
        if (t == typeof(string))
        {
            string s = (string)(FiOrValue is FieldInfo ? (IsReflectionOnly ? ((FieldInfo)FiOrValue).GetRawConstantValue() : ((FieldInfo)FiOrValue).GetValue(null)) : FiOrValue);
            return (s == null ? 0 : s.Length * 2);
        }
        if (t.IsEnum) t = Enum.GetUnderlyingType(t);
        return (!t.ContainsGenericParameters && !t.IsGenericType && (t.IsValueType || t.IsPointer || t.IsLayoutSequential) ? System.Runtime.InteropServices.Marshal.SizeOf(t) : 0);
    }

    static bool FileContentsMatch(string path1, string path2)
    {
        FileInfo fi1 = new FileInfo(path1), fi2 = new FileInfo(path2);
        if (!fi1.Exists || !fi2.Exists || fi1.Length != fi2.Length) return false;
        FileStream stream1 = fi1.Open(FileMode.Open, FileAccess.Read, FileShare.Read), stream2 = fi2.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        for (byte[] buf1 = new byte[4096], buf2 = new byte[4096];;)
        {
            int count = stream1.Read(buf1, 0, 4096); stream2.Read(buf2, 0, 4096);
            if (count == 0) return true;
            for (int i = 0; i < count; i += sizeof(Int64))
                if (BitConverter.ToInt64(buf1, i) != BitConverter.ToInt64(buf2, i))
                    return false;
        }
    }

    //PInvoke definitions for Win32 resource enumeration
    enum LoadLibraryFlags : uint { LOAD_LIBRARY_AS_DATAFILE = 2 };
    enum ResType { Cursor = 1, Bitmap, Icon, Menu, Dialog, String, FontDir, Font, Accelerator, RCData, MessageTable, CursorGroup, IconGroup = 14, VersionInfo = 16, DLGInclude, PlugPlay = 19, VXD, AnimatedCursor, AnimatedIcon, HTML, Manifest };
    delegate bool EnumResTypeProc(IntPtr hModule, IntPtr lpszType, IntPtr lParam);
    delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeProc lpEnumFunc, IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern bool EnumResourceNames(IntPtr hModule, int dwID, EnumResNameProc lpEnumFunc, IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("Kernel32.dll")] static extern IntPtr FindResource(IntPtr hModule, IntPtr lpszName, IntPtr lpszType);
    [System.Runtime.InteropServices.DllImport("Kernel32.dll")] static extern uint SizeofResource(IntPtr hModule, IntPtr hResource);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr hModule);
}
