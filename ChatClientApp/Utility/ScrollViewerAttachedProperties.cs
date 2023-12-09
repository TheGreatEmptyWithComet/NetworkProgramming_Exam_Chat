﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace ChatClientApp
{
    public static class ScrollViewerAttachedProperties
    {
        public static readonly DependencyProperty ScrollToBottomOnChangeProperty = DependencyProperty.RegisterAttached(
            "ScrollToBottomOnChange", typeof(object), typeof(ScrollViewerAttachedProperties), new PropertyMetadata(default(ScrollViewer), OnScrollToBottomOnChangeChanged));

        private static void OnScrollToBottomOnChangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var scrollViewer = dependencyObject as ScrollViewer;
            scrollViewer?.ScrollToBottom();
        }

        public static void SetScrollToBottomOnChange(DependencyObject element, object value)
        {
            element.SetValue(ScrollToBottomOnChangeProperty, value);
        }

        public static object GetScrollToBottomOnChange(DependencyObject element)
        {
            return element.GetValue(ScrollToBottomOnChangeProperty);
        }
    }
}
