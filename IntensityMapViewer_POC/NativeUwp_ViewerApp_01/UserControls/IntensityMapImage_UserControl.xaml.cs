﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SkiaUtilities;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics.CodeAnalysis;
using Common.ExtensionMethods;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace NativeUwp_ViewerApp_01
{

  public record ReferencePositionChangedMessage ( int ? X, int ? Y ) ;

  public record PointerPositionChangedMessage ( int ? X, int ? Y ) ;

  public sealed partial class IntensityMapImage_UserControl : UserControl
  {

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
      "ViewModel", 
      typeof(IntensityMapViewer.ISourceViewModel), 
      typeof(IntensityMapImage_UserControl), 
      new PropertyMetadata(
        defaultValue : null,
        propertyChangedCallback : (dp,propertyChangedEventArgs) => {
          var userControlThatOwnsThisViewModelProperty = dp as IntensityMapImage_UserControl ;
          userControlThatOwnsThisViewModelProperty.OnViewModelPropertyChanged(
            propertyChangedEventArgs.OldValue as IntensityMapViewer.ISourceViewModel,
            propertyChangedEventArgs.NewValue as IntensityMapViewer.ISourceViewModel
          ) ;
        }
      )
    ) ;

    public IntensityMapViewer.ISourceViewModel ViewModel
    {
      get => GetValue(ViewModelProperty) as IntensityMapViewer.ISourceViewModel ;
      set => SetValue(ViewModelProperty,value) ;
    }

    private UwpSkiaUtilities.PanAndZoomGesturesHandler m_panAndZoomGesturesHandler ;

    public static bool SupportPanAndZoom = true ;

    public IntensityMapImage_UserControl ( )
    {
      InitializeComponent() ;
    }

    private void OnViewModelPropertyChanged ( 
      IntensityMapViewer.ISourceViewModel? oldViewModel,
      IntensityMapViewer.ISourceViewModel? newViewModel
    ) {
      if ( SupportPanAndZoom )
      {
        m_panAndZoomGesturesHandler = new(
          m_skiaCanvas,
          new SkiaSceneRenderer(DrawIntensityMap){
            ShowTransformMatrixInfo = true,
            RenderHook = (canvas) => {
              SkiaSceneRenderer.LoadPanAndZoomParameters(
                newViewModel.Parent.PanAndZoomParameters,
                canvas.TotalMatrix
              ) ;
            },
          }
        ) {
          TouchActionDetected = TouchActionDetected
        } ;
      }
      else
      {
        m_skiaCanvas.PaintSurface += DrawSkiaContent ;
      }
      newViewModel.NewIntensityMapAcquired += () => PerformRepaint() ;
      newViewModel.ProfileDisplaySettings.ProfileGraphsReferencePositionChanged += () => {
        Common.DebugHelpers.WriteDebugLines(
          $"ProfileGraphsReferencePositionChanged => {newViewModel.ProfileDisplaySettings.ProfileGraphsReferencePosition}"
        ) ;
        PerformRepaint() ;
      } ;
      ViewModel.Parent.ImagePresentationSettings.PropertyChanged += (s,e) => {
        PerformRepaint() ;
      } ;
    }

    private void PerformRepaint ( )
    {
      m_skiaCanvas.Invalidate() ;
    }

    //
    // Dragging works as follows :
    //
    // When a 'touch' occurs, it is raised as an event on the skia Canvas.
    // Our 'PanAndZoomAndRotationGesturesHandler' will have hooked into that event,
    // and by default will pass it on to a helper class that will perform adjustments
    // to the Transform Matrix as required to implement panning and zooming.
    //
    // Ordinarily we'll allow the default 'pan/zoom' actions to be performed.
    // However under certain circimstances, we'll disable that behaviour and instead
    // use 'drag' operations to adjust the Reference Position.
    //
    
    private System.Drawing.Point? m_profileGraphsReferencePositionBeforeDragStarted = null ;

    private bool m_horizontalLineDraggingInProgress = false ;

    private bool m_verticalLineDraggingInProgress   = false ;

    private SkiaSharp.SKPoint? m_mostRecentlyNotifiedPointerPosition_sceneCoordinates = null ;
    private bool               m_inContact                                            = false ;

    private bool TouchActionDetected (
      TouchTracking.TouchActionType actionType, 
      SkiaSharp.SKPoint             positionInSceneCoordinates,
      bool                          inContact
    ) {
      bool handled = false ;
      m_inContact = inContact ;
      switch ( actionType )
      {
      case TouchTracking.TouchActionType.Entered:
        m_mostRecentlyNotifiedPointerPosition_sceneCoordinates = positionInSceneCoordinates ;
        break ;
      case TouchTracking.TouchActionType.Pressed:
        m_mostRecentlyNotifiedPointerPosition_sceneCoordinates = positionInSceneCoordinates ;
        if ( 
           m_horizontalLine?.CoincidesWithMousePosition(m_mostRecentlyNotifiedPointerPosition_sceneCoordinates) is true  
        && ViewModel.ProfileDisplaySettings.ProfileGraphsReferencePosition.HasValue 
        ) {
          m_profileGraphsReferencePositionBeforeDragStarted = ViewModel.ProfileDisplaySettings.ProfileGraphsReferencePosition.Value ;
          m_horizontalLineDraggingInProgress = true ;
          handled = true ;
        }
        if ( 
           m_verticalLine?.CoincidesWithMousePosition(m_mostRecentlyNotifiedPointerPosition_sceneCoordinates) is true  
        && ViewModel.ProfileDisplaySettings.ProfileGraphsReferencePosition.HasValue 
        ) {
          m_profileGraphsReferencePositionBeforeDragStarted = ViewModel.ProfileDisplaySettings.ProfileGraphsReferencePosition.Value ;
          m_verticalLineDraggingInProgress = true ;
          handled = true ;
        }
        break ;
      case TouchTracking.TouchActionType.Moved:
        m_mostRecentlyNotifiedPointerPosition_sceneCoordinates = positionInSceneCoordinates ;
        int deltaRight = 0 ;
        int deltaDown = 0 ;
        if ( m_horizontalLineDraggingInProgress )
        {
          if ( 
            m_pixelToSceneCoordinatesMapper.CanGetPointInPixelCoordinates(
              m_mostRecentlyNotifiedPointerPosition_sceneCoordinates,
              out var mostRecentlyNotifiedPointerPosition_pixelCoordinates
            )
          ) {
            deltaDown = (
              mostRecentlyNotifiedPointerPosition_pixelCoordinates.Value.Y
            - m_profileGraphsReferencePositionBeforeDragStarted.Value.Y
            ) ;
          }
        }
        if ( m_verticalLineDraggingInProgress )
        {
          if ( 
            m_pixelToSceneCoordinatesMapper.CanGetPointInPixelCoordinates(
              m_mostRecentlyNotifiedPointerPosition_sceneCoordinates,
              out var mostRecentlyNotifiedPointerPosition_pixelCoordinates
            )
          ) {
            deltaRight = (
              mostRecentlyNotifiedPointerPosition_pixelCoordinates.Value.X
            - m_profileGraphsReferencePositionBeforeDragStarted.Value.X
            ) ;
          }
        }
        if ( 
           deltaRight != 0 
        || deltaDown  != 0
        ) {
          var updatedReferencePosition = new System.Drawing.Point(
            (
              m_profileGraphsReferencePositionBeforeDragStarted.Value.X + deltaRight
            ).ClampedToInclusiveRange(
              0,
              ViewModel.MostRecentlyAcquiredIntensityMap.Dimensions.Width - 1
            ),
            (
              m_profileGraphsReferencePositionBeforeDragStarted.Value.Y + deltaDown
            ).ClampedToInclusiveRange(
              0,
              ViewModel.MostRecentlyAcquiredIntensityMap.Dimensions.Height - 1
            )
          ) ;
          Common.DebugHelpers.WriteDebugLines(
            $"Adjusting ProfileGraphsReferencePosition by [{deltaRight},{deltaDown}] => [{updatedReferencePosition.X},{updatedReferencePosition.Y}]"
          ) ;
          ViewModel.ProfileDisplaySettings.ProfileGraphsReferencePosition = updatedReferencePosition ;
          Microsoft.Toolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(
            new ReferencePositionChangedMessage(
              updatedReferencePosition.X,
              updatedReferencePosition.Y
            )
          ) ;
        }
        break ;
      case TouchTracking.TouchActionType.Released:
        m_profileGraphsReferencePositionBeforeDragStarted = null ;
        m_horizontalLineDraggingInProgress = false ;
        m_verticalLineDraggingInProgress = false ;
        break ;
      case TouchTracking.TouchActionType.Cancelled:
        break ;
      case TouchTracking.TouchActionType.Exited:
        m_mostRecentlyNotifiedPointerPosition_sceneCoordinates = null ;
        break ;
      }
      PerformRepaint() ;
      return handled ;
    }

    private void DrawSkiaContent ( 
      object                                      sender, 
      SkiaSharp.Views.UWP.SKPaintSurfaceEventArgs paintSurfaceEventArgs
    ) {
      SkiaSharp.SKCanvas skiaCanvas = paintSurfaceEventArgs.Surface.Canvas ;
      SkiaSharp.SKRectI deviceClipBounds = skiaCanvas.DeviceClipBounds ;
      Common.DebugHelpers.WriteDebugLines(
        $"Skia.Canvas.DeviceClipBounds : [{deviceClipBounds.Left},{deviceClipBounds.Top}] size [{deviceClipBounds.Width}x{deviceClipBounds.Height}]"
      ) ;
      // All we need is the Canvas - we can query the ImageInfo to get the dimensions
      // but that gives us the same info we'll get from the 'device clip bounds'
      SkiaSharp.SKImageInfo imageInfo = paintSurfaceEventArgs.Info ;
      Common.DebugHelpers.WriteDebugLines(
        $"SKImageInfo : size [{imageInfo.Width}x{imageInfo.Height}]"
      ) ;
      // SkiaSharp.SKRect localClipBounds = skiaCanvas.LocalClipBounds ;
      // Common.DebugHelpers.WriteDebugLines(
      //   $"Skia.Canvas.LocalClipBounds : [{localClipBounds.Left},{localClipBounds.Top}] size [{localClipBounds.Width}x{localClipBounds.Height}]"
      // ) ;
      DrawIntensityMap(skiaCanvas) ;
    }

    private SkiaUtilities.HorizontalLine? m_horizontalLine = null ;

    private SkiaUtilities.VerticalLine?   m_verticalLine   = null ;

    private SkiaUtilities.PixelToSceneCoordinatesMapper m_pixelToSceneCoordinatesMapper ;

    private void DrawIntensityMap ( SkiaSharp.SKCanvas skiaCanvas )
    { 
      ViewModel.Parent.RaiseIntensityMapVisualisationHasChangedEvent() ;
      var deviceClipBounds = skiaCanvas.DeviceClipBounds ;
      if ( ViewModel != null )
      { 
        // Hmm, should try to eliminate this test ...
        var intensityMap = ViewModel.MostRecentlyAcquiredIntensityMap ;
        var bitmap = new SkiaSharp.SKBitmap(
          intensityMap.Dimensions.Width,
          intensityMap.Dimensions.Height
        ) ;
        var colourMapOption = ViewModel.Parent.ImagePresentationSettings.ColourMapOption ;
        var colourMapper = IntensityMapViewer.ColourMapper.InstanceFor(colourMapOption) ;
        var normalisationValue = ViewModel.Parent.ImagePresentationSettings.NormalisationValue ;
        var normalisationGainValue = 255.0 / normalisationValue ;
        byte ApplyNormalisationValue ( byte nominalIntensity )
        => (
          nominalIntensity >= normalisationValue
          ? (byte) 255
          : (byte) ( nominalIntensity * normalisationGainValue )
        ) ;
        bitmap.Pixels = intensityMap.IntensityValues.Select(
          intensity => new SkiaSharp.SKColor(
            colourMapper.MapByteValueToEncodedARGB(
              ApplyNormalisationValue(intensity)
            )
            // red   : intensity,
            // green : intensity,
            // blue  : intensity
          )
        ).ToArray() ;
        // We'll want to preserve the aspect ratio
        float expansionFactorX = deviceClipBounds.Width  / (float) intensityMap.Dimensions.Width ;
        float expansionFactorY = deviceClipBounds.Height / (float) intensityMap.Dimensions.Height ;
        float expansionFactor = System.MathF.Min(
          expansionFactorX,
          expansionFactorY
        ) ;
        SkiaSharp.SKRect rectInWhichToDrawBitmap = new SkiaSharp.SKRect(
          left   : 0.0f,
          top    : 0.0f,
          // right  : deviceClipBounds.Width,
          right : intensityMap.Dimensions.Width * expansionFactor, 
          // bottom : deviceClipBounds.Height
          bottom : intensityMap.Dimensions.Height * expansionFactor 
        ) ;
        m_pixelToSceneCoordinatesMapper = new SkiaUtilities.PixelToSceneCoordinatesMapper(
          intensityMap.Dimensions,
          rectInWhichToDrawBitmap.Size
        ) ;
        skiaCanvas.DrawBitmap(
          bitmap,
          rectInWhichToDrawBitmap
        ) ;
        var dragMarkerStyle = new SkiaSharp.SKPaint(){
          Color       = SkiaSharp.SKColors.Blue,
          StrokeWidth = 3
        } ;
        float zoomCompensationFactor = 1.0f / skiaCanvas.TotalMatrix.ScaleX ; 
        if ( m_mostRecentlyNotifiedPointerPosition_sceneCoordinates.HasValue )
        {
          float ovalDiameter = 4.0f ;
          skiaCanvas.DrawOval(
            m_mostRecentlyNotifiedPointerPosition_sceneCoordinates.Value,
            new SkiaSharp.SKSize(
              ovalDiameter * zoomCompensationFactor,
              ovalDiameter * zoomCompensationFactor
              // m_inContact ? 10.0f : 5.0f,
              // m_inContact ? 10.0f : 5.0f
            ),
            dragMarkerStyle
          ) ;
        }
        if ( 
          m_pixelToSceneCoordinatesMapper.CanGetPointInSceneCoordinates(
            ViewModel.ProfileDisplaySettings.ProfileGraphsReferencePosition,
            out var referencePointInSceneCoordinates
          )
        ) {
          // m_pixelToSceneCoordinatesMapper.CanGetPointInPixelCoordinates(
          //   referencePointInSceneCoordinates,
          //   out var referencePositionInPixels 
          // ) ;
          m_horizontalLine = new SkiaUtilities.HorizontalLine(
            referencePointInSceneCoordinates.Value,
            0.0f,
            rectInWhichToDrawBitmap.Width
          ) ;
          m_verticalLine = new SkiaUtilities.VerticalLine(
            referencePointInSceneCoordinates.Value,
            0.0f,
            rectInWhichToDrawBitmap.Height
          ) ;
          var horizontalLineStyle = new SkiaSharp.SKPaint(){
            Color       = SkiaSharp.SKColors.Red,
            StrokeWidth = zoomCompensationFactor * (
              m_horizontalLine.CoincidesWithMousePosition(m_mostRecentlyNotifiedPointerPosition_sceneCoordinates)  
              ? 2
              : 1 
            )
          } ;        
          var verticalLineStyle = new SkiaSharp.SKPaint(){
            Color       = SkiaSharp.SKColors.Red,
            StrokeWidth = zoomCompensationFactor * (
              m_verticalLine.CoincidesWithMousePosition(m_mostRecentlyNotifiedPointerPosition_sceneCoordinates)   
              ? 2
              : 1
            )
          } ;
          m_horizontalLine.Draw(skiaCanvas,horizontalLineStyle) ;
          m_verticalLine.Draw(skiaCanvas,verticalLineStyle) ;
        }
        else
        {
          m_horizontalLine = null ;
          m_verticalLine   = null ;
        }
        SkiaSharp.SKPaint textPaint = new SkiaSharp.SKPaint() { 
          Color       = SkiaSharp.SKColors.White,
          IsAntialias = true,
          TextSize    = zoomCompensationFactor * 16.0f,
          Typeface    = SkiaSharp.SKTypeface.FromFamilyName(
            "Courier",
            SkiaSharp.SKFontStyle.Normal
          )
        } ;
        // skiaCanvas.DrawCircle(
        //  cx     : 0.0f,
        //  cy     : 0.0f,
        //  radius : 10.0f,
        //  redPaint
        //  ;
        if ( 
           m_mostRecentlyNotifiedPointerPosition_sceneCoordinates.HasValue
        && m_pixelToSceneCoordinatesMapper.CanGetPointInPixelCoordinates(
             m_mostRecentlyNotifiedPointerPosition_sceneCoordinates,
             out var pointerPositionInPixels 
           )
        ) { 
          var intensityValue = ViewModel.MostRecentlyAcquiredIntensityMap.GetIntensityValueAt(
            pointerPositionInPixels.Value.X,
            pointerPositionInPixels.Value.Y
          ) ;
          // string label = $"[{ViewModel.ProfileDisplaySettings.ProfileGraphsReferencePosition.Value.X},{ViewModel.ProfileDisplaySettings.ProfileGraphsReferencePosition.Value.Y}] : {intensityValue}" ;
          string label = $"{pointerPositionInPixels.Value.ToPixelPositionString()} {intensityValue}" ;
          skiaCanvas.DrawText(
            label,
            m_mostRecentlyNotifiedPointerPosition_sceneCoordinates.Value.MovedBy(
              +10.0f * zoomCompensationFactor,
              -20.0f * zoomCompensationFactor
            ),
            textPaint
          ) ;
        }

      }

    }

  }

}
