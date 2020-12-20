﻿//
// IntensityMapTestPage.cs
//

namespace IntensityMapViewer
{

  public sealed partial class IntensityMapTestPage : Windows.UI.Xaml.Controls.Page
  {

    // private IntensityMapTestViewModel ViewModel = new() ;

    private NativeUwp_TestHarnessApp.ViewModels.IntensityMapTestViewModel ViewModel
    => NativeUwp_TestHarnessApp.ViewModels.ViewModelLocator.Current.IntensityMapTestViewModel ; 

    public IntensityMapTestPage ( )
    {
      this.InitializeComponent() ;
    }

  }

}