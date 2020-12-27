﻿//
// IntensityMapTestViewModel.cs
//

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IntensityMapViewer
{

  //
  // This drives a UI that lets us exercise the visualisation of an IntensityMap.
  //
  // It provides a fixed (ie not time-varying) image from a selectable source eg
  //  - Ripple pattern, synthesised from fixed parameters
  //  - Loaded from a 'pgm' file
  //  - Loaded from EPICS (eventually)
  // ... with a selectable colour map :
  //   Solid colour (grey,r,g,b)
  //   JET colour
  //

  public class StaticIntensityMapsDemo_ViewModel 
  : IntensityMapsDemo_ViewModel
  {

    private Common.CyclicSelector<(IIntensityMap,ColourMapOption,string)> m_staticImagesSelector = new(
      (
        new IntensityMap.CreatedAsUniformPixelValue(),
        ColourMapOption.ShadesOfRed,
        "Solid red"
      ),
      (
        new IntensityMap.CreatedAsUniformPixelValue(),
        ColourMapOption.ShadesOfGreen,
        "Solid green"
      ),
      (
        new IntensityMap.CreatedAsUniformPixelValue(),
        ColourMapOption.ShadesOfBlue,
        "Solid blue"
      ),
      (
        new IntensityMap.CreatedAsOffsettedCircle(),
        ColourMapOption.ShadesOfBlue,
        "Offsetted circle"
      )
      // (
      //   UwpUtilities.BitmapHelpers_ForTesting.CreateWriteableBitmap_ForTesting_B(
      //     (x,y) => (0xff,(byte)(x*2),(byte)(y*2))
      //   ), 
      //   "Snazzy"
      // ),
      // (
      //   UwpUtilities.BitmapHelpers.CreateWriteableBitmap(
      //     intensityMap : 
      //     new IntensityMapViewer.IntensityMap.CreatedAsOffsettedCircle(),
      //     colourMapOption : IntensityMapViewer.ColourMapOption.GreyScale
      //   ),
      //   "Synthesised greyscale blob"
      // ),
      // (
      //   UwpUtilities.BitmapHelpers.CreateWriteableBitmap(
      //     intensityMap : 
      //     new IntensityMapViewer.IntensityMap.CreatedFromSincFunction(),
      //     colourMapOption : IntensityMapViewer.ColourMapOption.JetColours
      //   ),
      //   "Synthesised ripple with JET colours"
      // ),
      // (
      //   UwpUtilities.BitmapHelpers.CreateWriteableBitmap(
      //     intensityMap : new IntensityMapViewer.IntensityMap.CreatedFromSincFunction(
      //       sincFactor : 5.0
      //     ),
      //     colourMapOption : IntensityMapViewer.ColourMapOption.GreyScale
      //   ),
      //   "Synthesised ripple with greyscale"
      // ),
      // (
      //   UwpUtilities.BitmapHelpers.CreateWriteableBitmap(
      //     intensityMap : new IntensityMapViewer.IntensityMap.CreatedFromSincFunction(
      //       sincFactor                       : 15.0,
      //       fractionalRadialOffsetFromCentre : 0.2
      //     ).CreateCloneWithAddedRandomNoise(50),
      //     colourMapOption : IntensityMapViewer.ColourMapOption.ShadesOfRed
      //   ),
      //   "Coloured with shades of red, offset from centre, with noise"
      // ),
      // (
      //   UwpUtilities.BitmapHelpers.CreateWriteableBitmap(
      //     intensityMap : new IntensityMapViewer.IntensityMap.CreatedWithRampingValues(),
      //     colourMapOption : IntensityMapViewer.ColourMapOption.ShadesOfBlue
      //   ),
      //   "Ramping, blue"
      // )
    ) ;

    public StaticIntensityMapsDemo_ViewModel ( )
    {
      MoveToNextStaticImage = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(
        () => (IntensityMap,ColourMapOption,IntensityMapLabel) = m_staticImagesSelector.GetCurrent_MoveNext()
      ) ;
      MoveToNextStaticImage.Execute(null) ;
    }

    public Microsoft.Toolkit.Mvvm.Input.IRelayCommand MoveToNextStaticImage { get ; }

  }

}
