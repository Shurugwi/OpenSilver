﻿

/*===================================================================================
* 
*   Copyright (c) Userware/OpenSilver.net
*      
*   This file is part of the OpenSilver Runtime (https://opensilver.net), which is
*   licensed under the MIT license: https://opensource.org/licenses/MIT
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/


//TODOBRIDGE: usefull using?
#if !BRIDGE
using JSIL.Meta;
#else
using Bridge;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Browser;

#if MIGRATION
using System.Windows.Controls.Primitives;
#else
using Windows.UI.Xaml.Controls.Primitives;
#endif

#if MIGRATION
namespace System.Windows.Controls
#else
namespace Windows.UI.Xaml.Controls
#endif
{
    /// <summary>
    /// Represents a button control that displays a hyperlink.
    /// </summary>
    /// <example>
    /// <code lang="XAML" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    /// <StackPanel x:Name="MyStackPanel">
    ///     <HyperlinkButton Content="here" NavigateUri="http://www.myaddress.com" Foreground="Blue"/>
    /// </StackPanel>
    /// </code>
    /// <code lang="C#">
    /// HyperlinkButton hyperlinkButton = new HyperlinkButton() { Content = "here", NavigateUri = new Uri("http://www.myaddress.com"), Foreground = new SolidColorBrush(Windows.UI.Colors.Blue) };
    /// MyStackPanel.Children.Add(hyperlinkButton);
    /// </code>
    /// </example>
    public partial class HyperlinkButton : ButtonBase
    {
        /// <summary>
        /// Initializes a new instance of the HyperlinkButton class.
        /// </summary>
        public HyperlinkButton()
        {
            // Set default style:
            this.DefaultStyleKey = typeof(HyperlinkButton);

            Click += HyperlinkButton_Click;
        }

        void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigateUri != null)
            {
                string targetName = this.TargetName;
                object targetElement = null;

                // Look for an element within the application (in the Visual Tree) that has the specified TargetName and implements INavigate:
                if (!string.IsNullOrEmpty(targetName)
                    && (targetElement = this.FindName(targetName)) is INavigate) //todo: verify that "FindName" is enough to find the element (it walks up the visual tree and looks in the elements that implement INameScope) or if we should manually traverse all the nodes of the Visual Tree.
                {
                    //-----------------
                    // Navigation within the application
                    //-----------------

                    ((INavigate)targetElement).Navigate(NavigateUri);
                }
                else
                {
                    //-----------------
                    // External navigation (browser navigation)
                    //-----------------

                    if (string.IsNullOrEmpty(targetName))
                    {
#if MIGRATION
                        targetName = "_self";
#else
                        targetName = "_blank";
#endif
                    }
                    HtmlPage.Window.Navigate(NavigateUri, targetName);
                }
            }
        }


        /// <summary>
        /// Gets or sets the Uniform Resource Identifier (URI) to navigate to when the
        /// HyperlinkButton is clicked.
        /// </summary>
        public Uri NavigateUri
        {
            get { return (Uri)GetValue(NavigateUriProperty); }
            set { SetValue(NavigateUriProperty, value); }
        }
        /// <summary>
        /// Identifies the NavigateUri dependency property.
        /// </summary>
        public static readonly DependencyProperty NavigateUriProperty =
            DependencyProperty.Register("NavigateUri", typeof(Uri), typeof(HyperlinkButton), new PropertyMetadata(null));



        /// <summary>
        /// Gets or sets the name of the target window or frame that the Web page should
        /// open in, or the name of the object within the application to navigate to.
        /// </summary>
        public string TargetName
        {
            get { return (string)GetValue(TargetNameProperty); }
            set { SetValue(TargetNameProperty, value); }
        }
        /// <summary>
        /// Identifies the TargetName dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetNameProperty =
            DependencyProperty.Register("TargetName", typeof(string), typeof(HyperlinkButton), new PropertyMetadata(""));


    }
}