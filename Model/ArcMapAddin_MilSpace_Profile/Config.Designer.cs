//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ArcMapAddin_MilSpace_Profile {
    using ESRI.ArcGIS.Framework;
    using ESRI.ArcGIS.ArcMapUI;
    using ESRI.ArcGIS.Editor;
    using ESRI.ArcGIS.esriSystem;
    using System;
    using System.Collections.Generic;
    using ESRI.ArcGIS.Desktop.AddIns;
    
    
    /// <summary>
    /// A class for looking up declarative information in the associated configuration xml file (.esriaddinx).
    /// </summary>
    internal static class ThisAddIn {
        
        internal static string Name {
            get {
                return "ArcMapAddin_MilSpace_Profile";
            }
        }
        
        internal static string AddInID {
            get {
                return "{a3ca6e5c-fac0-4b81-a812-562f1f59cba5}";
            }
        }
        
        internal static string Company {
            get {
                return "Microsoft";
            }
        }
        
        internal static string Version {
            get {
                return "1.0";
            }
        }
        
        internal static string Description {
            get {
                return "Type in a description for this Add-in.";
            }
        }
        
        internal static string Author {
            get {
                return "admin";
            }
        }
        
        internal static string Date {
            get {
                return "15.11.2018";
            }
        }
        
        internal static ESRI.ArcGIS.esriSystem.UID ToUID(this System.String id) {
            ESRI.ArcGIS.esriSystem.UID uid = new ESRI.ArcGIS.esriSystem.UIDClass();
            uid.Value = id;
            return uid;
        }
        
        /// <summary>
        /// A class for looking up Add-in id strings declared in the associated configuration xml file (.esriaddinx).
        /// </summary>
        internal class IDs {
            
            /// <summary>
            /// Returns 'Microsoft_ArcMapAddin_MilSpace_Profile_Button1', the id declared for Add-in Button class 'Button1'
            /// </summary>
            internal static string Button1 {
                get {
                    return "Microsoft_ArcMapAddin_MilSpace_Profile_Button1";
                }
            }
            
            /// <summary>
            /// Returns 'Microsoft_ArcMapAddin_MilSpace_Profile_DockableWindowMilSpaceProfileCalc', the id declared for Add-in DockableWindow class 'DockableWindowMilSpaceProfileCalc+AddinImpl'
            /// </summary>
            internal static string DockableWindowMilSpaceProfileCalc {
                get {
                    return "Microsoft_ArcMapAddin_MilSpace_Profile_DockableWindowMilSpaceProfileCalc";
                }
            }
            
            /// <summary>
            /// Returns 'Microsoft_ArcMapAddin_MilSpace_Profile_DockableWindowMilSpaceProfileGraph', the id declared for Add-in DockableWindow class 'DockableWindowMilSpaceProfileGraph+AddinImpl'
            /// </summary>
            internal static string DockableWindowMilSpaceProfileGraph {
                get {
                    return "Microsoft_ArcMapAddin_MilSpace_Profile_DockableWindowMilSpaceProfileGraph";
                }
            }
        }
    }
    
internal static class ArcMap
{
  private static IApplication s_app = null;
  private static IDocumentEvents_Event s_docEvent;

  public static IApplication Application
  {
    get
    {
      if (s_app == null)
      {
        s_app = Internal.AddInStartupObject.GetHook<IMxApplication>() as IApplication;
        if (s_app == null)
        {
          IEditor editorHost = Internal.AddInStartupObject.GetHook<IEditor>();
          if (editorHost != null)
            s_app = editorHost.Parent;
        }
      }
      return s_app;
    }
  }

  public static IMxDocument Document
  {
    get
    {
      if (Application != null)
        return Application.Document as IMxDocument;

      return null;
    }
  }
  public static IMxApplication ThisApplication
  {
    get { return Application as IMxApplication; }
  }
  public static IDockableWindowManager DockableWindowManager
  {
    get { return Application as IDockableWindowManager; }
  }
  public static IDocumentEvents_Event Events
  {
    get
    {
      s_docEvent = Document as IDocumentEvents_Event;
      return s_docEvent;
    }
  }
  public static IEditor Editor
  {
    get
    {
      UID editorUID = new UID();
      editorUID.Value = "esriEditor.Editor";
      return Application.FindExtensionByCLSID(editorUID) as IEditor;
    }
  }
}

namespace Internal
{
  [StartupObjectAttribute()]
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
  public sealed partial class AddInStartupObject : AddInEntryPoint
  {
    private static AddInStartupObject _sAddInHostManager;
    private List<object> m_addinHooks = null;

    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    public AddInStartupObject()
    {
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool Initialize(object hook)
    {
      bool createSingleton = _sAddInHostManager == null;
      if (createSingleton)
      {
        _sAddInHostManager = this;
        m_addinHooks = new List<object>();
        m_addinHooks.Add(hook);
      }
      else if (!_sAddInHostManager.m_addinHooks.Contains(hook))
        _sAddInHostManager.m_addinHooks.Add(hook);

      return createSingleton;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void Shutdown()
    {
      _sAddInHostManager = null;
      m_addinHooks = null;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static T GetHook<T>() where T : class
    {
      if (_sAddInHostManager != null)
      {
        foreach (object o in _sAddInHostManager.m_addinHooks)
        {
          if (o is T)
            return o as T;
        }
      }

      return null;
    }

    // Expose this instance of Add-in class externally
    public static AddInStartupObject GetThis()
    {
      return _sAddInHostManager;
    }
  }
}
}
