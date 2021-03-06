﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.InteractionTracker.Forms.Events;
using Xamarin.InteractionTracker.Forms.Models;

namespace Xamarin.InteractionTracker.Forms
{
    /// <summary>
    /// Implementation of tracker manager
    /// </summary>
    public class TrackerManager : ITrackerManager
    {
        public IList<UIEvent> EventCache { get; }
        public IList<View> ScannedViewsCache { get; }
        public GestureType GesturesTracked { get; }
        public bool IsUsingCache { get; }
        public event EventHandler<InteractionEventArgs> OnInteractionDetected;

        public TrackerManager(GestureType gesturesTracked, bool usingCache)
        {
            GesturesTracked = gesturesTracked;
            IsUsingCache = usingCache;
            if (usingCache)
            {
                EventCache = new List<UIEvent>();
            }
            ScannedViewsCache = new List<View>();
        }

        /// <summary>
        /// Scans a page for views to track interactinos with
        /// </summary>
        /// <param name="page"></param>
        /// <param name="clearViewCache"></param>
        /// <param name="includeLayouts"></param>
        public void ScanPage(ContentPage page, bool clearViewCache = false, bool includeLayouts = false)
        {
            if (clearViewCache)
            {
                ScannedViewsCache.Clear();
            }
            var rootView = page.Content;
            if (rootView is ILayoutController)
            {
                ScanLayout((ILayoutController)rootView, includeLayouts);
            }
            else if (!(rootView is Layout))
            {
                MonitorView(rootView);
            }
        }

        /// <summary>
        /// Scans a layout view for it's children. If the child view is a layout, it scans those views as well.
        /// </summary>
        /// <param name="rootView"></param>
        /// <param name="includeLayouts"></param>
        private void ScanLayout(ILayoutController rootView, bool includeLayouts)
        {
            foreach (var view in rootView.Children)
            {
                if (includeLayouts || !(view is Layout) && view is View)
                {
                    MonitorView((View)view);
                }
                if(view is Layout<View>)
                {
                    ScanLayout((Layout<View>)view, includeLayouts);
                }
            }
        }

        /// <summary>
        /// Adds gesture recognizers to a view and adds it to the view cache
        /// </summary>
        /// <param name="view"></param>
        private void MonitorView(View view)
        {
            switch (GesturesTracked)
            {
                case GestureType.Tap:
                    view.GestureRecognizers.Add(new TapGestureRecognizer
                    {
                        Command = new Command(() => TrackEvent(view, GestureType.Tap))
                    });
                    break;
                case GestureType.Pan:
                    var panGesture = new PanGestureRecognizer();
                    panGesture.PanUpdated += (s, e) =>
                    {
                        if (e.StatusType == GestureStatus.Completed)
                            TrackEvent(view, GestureType.Pan);
                    };
                    view.GestureRecognizers.Add(panGesture);
                    break;
                case GestureType.Pinch:
                    var pinchGesture = new PinchGestureRecognizer();
                    pinchGesture.PinchUpdated += (s, e) =>
                    {
                        if (e.Status == GestureStatus.Completed)
                            TrackEvent(view, GestureType.Pan);
                    };
                    view.GestureRecognizers.Add(pinchGesture);
                    break;
                case GestureType.All:
                default:
                    view.GestureRecognizers.Add(new TapGestureRecognizer
                    {
                        Command = new Command(() => TrackEvent(view, GestureType.Tap))
                    });
                    var panGestureAll = new PanGestureRecognizer();
                    panGestureAll.PanUpdated += (s, e) =>
                    {
                        if (e.StatusType == GestureStatus.Completed)
                            TrackEvent(view, GestureType.Pan);
                    };
                    view.GestureRecognizers.Add(panGestureAll);
                    var pinchGestureAll = new PinchGestureRecognizer();
                    pinchGestureAll.PinchUpdated += (s, e) =>
                    {
                        if (e.Status == GestureStatus.Completed)
                            TrackEvent(view, GestureType.Pan);
                    };
                    view.GestureRecognizers.Add(pinchGestureAll);
                    break;
            }
            ScannedViewsCache.Add(view);
        }

        /// <summary>
        /// Tracks an event from an interaction. Adds it to the cache if set, and fires event
        /// </summary>
        /// <param name="view"></param>
        /// <param name="gesture"></param>
        private void TrackEvent(View view, GestureType gesture)
        {
            var uiEvent = new UIEvent
            {
                Gesture = gesture,
                Id = Guid.NewGuid(),
                EventTime = DateTime.UtcNow,
                ViewObject = view
            };
            if (IsUsingCache)
            {
                EventCache.Add(uiEvent);
            }
            OnInteractionDetected?.Invoke(this, new InteractionEventArgs(uiEvent));
        }
    }
}
