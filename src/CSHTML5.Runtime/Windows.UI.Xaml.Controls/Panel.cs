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


using CSHTML5.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
#if MIGRATION
using System.Windows.Media;
#else
using Windows.UI.Xaml.Media;
using System.Windows.Controls;
#endif

#if MIGRATION
namespace System.Windows.Controls
#else
namespace Windows.UI.Xaml.Controls
#endif
{
    /// <summary>
    /// Provides a base class for all Panel elements. Use Panel elements to position
    /// and arrange child objects in a UI page.
    /// </summary>
    [ContentProperty("Children")]
    public abstract partial class Panel : FrameworkElement
    {
#if REVAMPPOINTEREVENTS
        internal override bool INTERNAL_ManageFrameworkElementPointerEventsAvailability()
        {
            // We only check the Background property even if BorderBrush not null + BorderThickness > 0 is a sufficient condition to enable pointer events on the borders of the control.
            // There is no way right now to differentiate the Background and BorderBrush as they are both defined on the same DOM element.
            return Background != null;
        }
#endif

        UIElementCollection _children;

        [Obsolete("Replaced by 'EnableProgressiveRendering'")]
        public bool INTERNAL_EnableProgressiveLoading
        {
            get { return this.EnableProgressiveRendering; }
            set { this.EnableProgressiveRendering = value; }
        }

        private bool _enableProgressiveRendering;
        public bool EnableProgressiveRendering
        {
            get { return this._enableProgressiveRendering || INTERNAL_ApplicationWideEnableProgressiveRendering; }
            set { this._enableProgressiveRendering = value; }
        }
        internal static bool INTERNAL_ApplicationWideEnableProgressiveRendering;

        private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.ManageChildrenChanged_v2(e.Action, e.OldStartingIndex, e.OldItems, e.NewStartingIndex, e.NewItems);
        }

        /// <summary>
        /// Gets or sets a Brush that is used to fill the panel.
        /// </summary>
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }
        /// <summary>
        /// Identifies the Background dependency property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register("Background", typeof(Brush), typeof(Panel), new PropertyMetadata(null
#if REVAMPPOINTEREVENTS
                , Background_Changed
#endif
                )
            {
                GetCSSEquivalent = (instance) =>
                {
                    return new CSSEquivalent()
                    {
                        Name = new List<string> { "background", "backgroundColor", "backgroundColorAlpha" },
                    };
                },
                CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.IfPropertyIsSet
            }
            );

#if REVAMPPOINTEREVENTS
        private static void Background_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = (UIElement)d;
            INTERNAL_UpdateCssPointerEvents(element);
        }
#endif

        /// <summary>
        /// Gets the collection of child elements of the panel.
        /// </summary>
        public UIElementCollection Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new UIElementCollection();

                    if (this._isLoaded)
                        _children.CollectionChanged += OnChildrenCollectionChanged;
                }

                return _children;
            }
        }


        protected internal override void INTERNAL_OnAttachedToVisualTree()
        {
            base.INTERNAL_OnAttachedToVisualTree();
            if (_children != null)
            {
                _children.CollectionChanged -= OnChildrenCollectionChanged;
                _children.CollectionChanged += OnChildrenCollectionChanged;
            }

            this.ManageChildrenChanged(this._children, this._children);
        }

        protected internal override void INTERNAL_OnDetachedFromVisualTree()
        {
            if (_children != null)
                _children.CollectionChanged -= OnChildrenCollectionChanged;
        }

        internal virtual void ManageChildrenChanged(IList oldChildren, IList newChildren)
        {
            if (oldChildren != null)
            {
                // Detach old children only if they are not in the "newChildren" collection:
                foreach (UIElement child in oldChildren) //note: there is no setter for Children so the user cannot change the order of the elements in one step --> we cannot have the same children in another order (which would keep the former order with the way it is handled now) --> no problem here
                {
                    if (newChildren == null || !newChildren.Contains(child))
                    {
                        INTERNAL_VisualTreeManager.DetachVisualChildIfNotNull(child, this);
                    }
                }
            }
            if (newChildren != null)
            {
                // Note: we attach all the children (regardless of whether they are in the oldChildren collection or not) to make it work when the item is first added to the Visual Tree (at that moment, all the properties are refreshed by calling their "Changed" method).

                if (this.EnableProgressiveRendering)
                {
                    this.ProgressivelyAttachChildren(newChildren);
                }
                else
                {
                    foreach (UIElement child in newChildren)
                    {
#if REWORKLOADED
                        this.AddVisualChild(child);
#else
                        INTERNAL_VisualTreeManager.AttachVisualChildIfNotAlreadyAttached(child, this);
#endif
                    }
                }
            }
        }

        internal virtual void ManageChildrenChanged_v2(NotifyCollectionChangedAction action, int oldIndex, IList oldChildren, int newIndex, IList newChildren)
        {
            //if newIndex > oldIndex && oldIndex >= 0 consider newIndex as newIndex + oldChildren.Count to compensate the fact that newIndex is considering without the oldElements
            //For every item in oldChildren add to hashSet to remove it.
            //For every item in newChildren:
            //  - if item in oldChildren remove from oldChildren's HashSet, and move it to newIndex + currentIndex in newChildren
            //  - else create and add the new child at the index of newIndex
            //  In both cases, increment currentIndex in NewChildren
            //For every item left in the oldChildren's HashSet, remove it.

            if (action == NotifyCollectionChangedAction.Move && newChildren == null)
            {
                //We need to do this because in the case of a move action, only the indexes and oldChildren are set and it doesn't fit with the logic of this method.
                newChildren = oldChildren;
            }
            bool compensateMovedItems = false;
            HashSet<UIElement> elementsToRemoveFromVisualTree = null;
            if (oldChildren != null)
            {
                if (newIndex > oldIndex && oldIndex > -1)
                {
                    compensateMovedItems = true;
                    newIndex += oldChildren.Count; //this is to compensate the fact that newIndex is the index after removing the oldChildren.
                }
                elementsToRemoveFromVisualTree = new HashSet<UIElement>();
                foreach (UIElement uiE in oldChildren)
                {
                    elementsToRemoveFromVisualTree.Add(uiE);
                }
            }
            else
            {
                elementsToRemoveFromVisualTree = new HashSet<UIElement>();
            }
            if (newChildren != null)
            {
                int indexInNewChildren = 0;
                int itemsMoved = 0; //Note: this is used to compensate the fact that the items were moved from a previous index to a further one so there are still the same amount of elements before the element that was 
                foreach (UIElement child in newChildren)
                {
                    if(elementsToRemoveFromVisualTree.Contains(child))
                    {
                        //we want to move the element instead of recreating it:
                        elementsToRemoveFromVisualTree.Remove(child);
                        INTERNAL_VisualTreeManager.MoveVisualChildInSameParent(child, this, newIndex + indexInNewChildren - itemsMoved);
                        if(compensateMovedItems)
                        {
                            ++itemsMoved;
                        }
                    }
                    else
                    {
#if REWORKLOADED
                        this.AddVisualChild(child, newIndex + indexInNewChildren - itemsMoved);
#else
                        INTERNAL_VisualTreeManager.AttachVisualChildIfNotAlreadyAttached(child, this, newIndex + indexInNewChildren - itemsMoved);
#endif
                    }
                    ++indexInNewChildren;
                }
            }

            foreach(UIElement child in elementsToRemoveFromVisualTree)
            {
                INTERNAL_VisualTreeManager.DetachVisualChildIfNotNull(child, this);
            }
        }

        private async void ProgressivelyAttachChildren(IList newChildren)
        {
            foreach (UIElement child in newChildren)
            {
                await Task.Delay(1);
                if(!INTERNAL_VisualTreeManager.IsElementInVisualTree(this))
                {
                    //this can happen if the Panel is detached during the delay.
                    break;
                }
#if REWORKLOADED
                this.AddVisualChild(child);
#else
                INTERNAL_VisualTreeManager.AttachVisualChildIfNotAlreadyAttached(child, this);
#endif
                INTERNAL_OnChildProgressivelyLoaded();
            }
        }

        protected virtual void INTERNAL_OnChildProgressivelyLoaded()
        {
        }


        /// <summary>
        /// Retrieves the named element in the instantiated ControlTemplate visual tree.
        /// </summary>
        /// <param name="childName">The name of the element to find.</param>
        /// <returns>
        /// The named element from the template, if the element is found. Can return
        /// null if no element with name childName was found in the template.
        /// </returns>
        protected internal DependencyObject GetTemplateChild(string childName)
        {
            return (DependencyObject)this.TryFindTemplateChildFromName(childName);
        }
#region ---------- INameScope implementation ----------
        //note: copy from UserControl
        Dictionary<string, object> _nameScopeDictionary = new Dictionary<string, object>();

        /// <summary>
        /// Finds the UIElement with the specified name. Returns null if not found.
        /// </summary>
        /// <param name="name">The name to look for.</param>
        /// <returns>The object with the specified name if any; otherwise null.</returns>
        private object TryFindTemplateChildFromName(string name)
        {
            //todo: see if this fits to the behaviour it should have.
            if (_nameScopeDictionary.ContainsKey(name))
                return _nameScopeDictionary[name];
            else
                return null;
        }

        public void RegisterName(string name, object scopedElement)
        {
            if (_nameScopeDictionary.ContainsKey(name) && _nameScopeDictionary[name] != scopedElement)
                throw new ArgumentException(string.Format("Cannot register duplicate name '{0}' in this scope.", name));

            _nameScopeDictionary[name] = scopedElement;
        }

        public void UnregisterName(string name)
        {
            if (!_nameScopeDictionary.ContainsKey(name))
                throw new ArgumentException(string.Format("Name '{0}' was not found.", name));

            _nameScopeDictionary.Remove(name);
        }

        void ClearRegisteredNames()
        {
            _nameScopeDictionary.Clear();
        }


        #endregion



        //internal override void INTERNAL_Render()
        //{
        //    base.INTERNAL_Render();

        //    if (Background is SolidColorBrush)
        //    {
        //        INTERNAL_HtmlDomManager.GetFrameworkElementOuterStyleForModification(this).backgroundColor = ((SolidColorBrush)Background).Color.INTERNAL_ToHtmlString();
        //    }
        //}

#if WORKINPROGRESS
        public static readonly DependencyProperty IsItemsHostProperty =
            DependencyProperty.Register("IsItemsHost",
                                        typeof(bool),
                                        typeof(Panel),
                                        new PropertyMetadata(false));
        public bool IsItemsHost
        {
            get { return (bool)this.GetValue(IsItemsHostProperty); }
            internal set { this.SetValue(IsItemsHostProperty, value); }
        }
#endif
    }
}
