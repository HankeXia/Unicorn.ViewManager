﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Linq;

namespace Unicorn.ViewManager
{
    [TemplatePart(Name = PART_POPUPSTACKPRESENTER, Type = typeof(ContentPresenter))]
    public class PopupStackControl : Control, IPopupItemContainer
    {
        const string PART_POPUPSTACKPRESENTER = "PART_POPUPSTACKPRESENTER";

        private ContentPresenter _stackPresenter = null;
        private readonly PopupStack _popupStack = null;

        public IEnumerable<PopupItem> Items
        {
            get
            {
                return this._popupStack.Items.Cast<PopupItemContainer>().Select(_p => _p.PopupItem);
            }
        }

        public PopupItem TopItem
        {
            get
            {
                return this.PopupItemFromIndex(this._popupStack.Items.Count - 1);
            }
        }

        static PopupStackControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupStackControl), new FrameworkPropertyMetadata(typeof(PopupStackControl)));

            CommandManager.RegisterClassCommandBinding(typeof(PopupStackControl), new CommandBinding(ViewCommands.ShowPopupItem, new ExecutedRoutedEventHandler(PopupStackControl.OnShowPopupItem), new CanExecuteRoutedEventHandler(PopupStackControl.OnCanShowPopupItem)));
            CommandManager.RegisterClassCommandBinding(typeof(PopupStackControl), new CommandBinding(ViewCommands.ClosePopupItem, new ExecutedRoutedEventHandler(PopupStackControl.OnClosePopupItem), new CanExecuteRoutedEventHandler(PopupStackControl.OnCanClosePopupItem)));
        }

        public PopupStackControl()
        {
            this._popupStack = new PopupStack(this);
        }

        private static void OnCanClosePopupItem(object sender, CanExecuteRoutedEventArgs e)
        {
            PopupStackControl stackControl = (PopupStackControl)sender;
            e.CanExecute = e.Parameter is PopupItem item && stackControl.Contains(item);
            e.Handled = true;
        }

        private static void OnClosePopupItem(object sender, ExecutedRoutedEventArgs e)
        {
            PopupStackControl stackControl = (PopupStackControl)sender;
            stackControl.Close((PopupItem)e.Parameter);
            e.Handled = true;
        }

        private static void OnCanShowPopupItem(object sender, CanExecuteRoutedEventArgs e)
        {
            PopupStackControl stackControl = (PopupStackControl)sender;
            e.CanExecute = e.Parameter is PopupItem item && !stackControl.Contains(item);
            e.Handled = true;
        }

        private static void OnShowPopupItem(object sender, ExecutedRoutedEventArgs e)
        {
            PopupStackControl stackControl = (PopupStackControl)sender;
            stackControl.Show((PopupItem)e.Parameter);
            e.Handled = true;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this._stackPresenter = this.GetTemplateChild(PART_POPUPSTACKPRESENTER) as ContentPresenter;

            if (this._stackPresenter == null)
            {
                throw new Exception($"模板缺少名为 {PART_POPUPSTACKPRESENTER} 且类型为 {typeof(ContentPresenter)} 的组件，该组件为视图容器");
            }

            this._stackPresenter.Content = this._popupStack;
        }

        private bool VerifyTopItemModal()
        {
            if (this._popupStack.Items.Count > 0)
            {
                var topitem = this.PopupItemFromIndex(this._popupStack.Items.Count - 1);

                if (topitem._showingAsModal)
                {
                    return true;
                }
            }

            return false;
        }

        private bool VerifyIsMessageDialogBox(PopupItem item)
        {
            return item is MessageDialogBox;
        }

        private bool VerifyIsProcessDialogBox(PopupItem item)
        {
            return item is ProcessDialogBox;
        }

        private bool VerifyIsSpecialItem(PopupItem item)
        {
            return item is MessageDialogBox
                || item is ProcessDialogBox;
        }

        private bool VerifyTopItemIsMessageDialogBox()
        {
            if (this._popupStack.Items.Count > 0)
            {
                var topitem = this.PopupItemFromIndex(this._popupStack.Items.Count - 1);

                return topitem is MessageDialogBox;
            }

            return false;
        }

        public bool Contains(PopupItem item)
        {
            foreach (PopupItemContainer container in this._popupStack.Items)
            {
                if (object.ReferenceEquals(container.PopupItem, item))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddItem(PopupItem item)
        {
            PopupItemContainer container = item.GetContainer();
            container.PopupItem = item;
            this._popupStack.Items.Add(container);
            item._isHostAtViewStack = true;
        }

        private void RemoveItem(PopupItem item)
        {
            var container = this.PopupContainerFromItem(item);
            this._popupStack.Items.Remove(container);
            item._isHostAtViewStack = false;
        }

        public PopupItemContainer PopupContainerFromIndex(int index)
        {
            return this._popupStack.ItemContainerGenerator.ContainerFromIndex(index) as PopupItemContainer;
        }

        public PopupItemContainer PopupContainerFromItem(PopupItem item)
        {
            foreach (PopupItemContainer container in this._popupStack.Items)
            {
                if (object.ReferenceEquals(container.PopupItem, item))
                {
                    return container;
                }
            }

            return null;
        }

        public PopupItem PopupItemFromIndex(int index)
        {
            return this.PopupContainerFromIndex(index).PopupItem;
        }

        private void MoveItemToTop(PopupItem item)
        {
            if (this.Contains(item)
                && !object.ReferenceEquals(this.TopItem, item))
            {
                PopupItemContainer container = this.PopupContainerFromItem(item);
                this._popupStack.Items.Remove(container);
                this._popupStack.Items.Add(container);
                container.OnShowAnimation(null);
            }
        }

        public ModalResult ShowModal(PopupItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (this.VerifyIsProcessDialogBox(item))
            {
                throw new Exception($"视图类型 {typeof(ProcessDialogBox)} 不可以模态方式进行显示。");
            }

            //以模态显示窗口时，若发现最上层是模态窗口
            //当顶级窗口为MessageDialogBox时不予显示当前窗口
            //当顶级窗口不是MessageDialogBox时，若当前为MessageDialogBox，则显示
            if (this.VerifyTopItemModal())
            {
                if (this.VerifyTopItemIsMessageDialogBox()
                    || !this.VerifyIsMessageDialogBox(item))
                {
                    PopupItemContainer container = this.PopupContainerFromIndex(this._popupStack.Items.Count - 1);
                    if (container != null)
                    {
                        container.Flicker();
                    }

                    return null;
                }
            }

            item.VerifyCanShow(this);

            item._showingAsModal = true;
            item._modalResult = null;
            CancelEventArgs ce = null;
            try
            {
                item.InternalShowing(out ce);
            }
            catch (Exception)
            {
                item._showingAsModal = false;
                throw;
            }

            try
            {
                if (!ce.Cancel)
                {
                    ComponentDispatcher.PushModal();
                    item._dispatcherFrame = new DispatcherFrame();
                    item.ParentHostStack = this;
                    item._isClosed = false;
                    this.AddItem(item);
                    item.InternalShown(out EventArgs e);
                    Dispatcher.PushFrame(item._dispatcherFrame);
                    return item.ModalResult;
                }
            }
            finally
            {
                ComponentDispatcher.PopModal();
                item.InternalDiapose();
            }

            return null;
        }

        public void Show(PopupItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (this.VerifyIsMessageDialogBox(item))
            {
                throw new Exception($"视图类型 {typeof(MessageDialogBox)} 必须以模态方式进行显示。");
            }

            //堆栈最上层若是模态窗口，则不予显示当前窗口，并激活最上层的模态窗口

            if (this.VerifyTopItemModal())
            {
                if (!this.VerifyIsProcessDialogBox(item))
                {
                    PopupItemContainer container = this.PopupContainerFromIndex(this._popupStack.Items.Count - 1);
                    if (container != null)
                    {
                        container.Flicker();
                    }

                    return;
                }
            }

            if (!this.Contains(item))
            {
                item.VerifyCanShow(this);

                item.InternalShowing(out CancelEventArgs ce);
                if (!ce.Cancel)
                {
                    item._isClosed = false;
                    item.ParentHostStack = this;
                    this.AddItem(item);
                    item.InternalShown(out EventArgs e);
                }
            }
            else
            {
                this.MoveItemToTop(item);
            }
        }

        public void Close(PopupItem item)
        {
            if (item == null)
            {
                return;
            }

            if (item._isClosing
                || item._isClosed)
            {
                return;
            }

            if (!this.Contains(item))
            {
                throw new Exception("该项当前不可关闭，因为它不处于当前的试图堆栈");
            }

            item._isClosing = true;
            CancelEventArgs ce = null;

            try
            {
                item.InternalClosing(out ce);
            }
            catch (Exception)
            {
                item._isClosing = false;
                throw;
            }


            void CloseAndDisposeItem(PopupItem popupItem)
            {
                this.RemoveItem(popupItem);
                try
                {
                    popupItem.InternalClosed(out EventArgs e);
                }
                finally
                {
                    popupItem.InternalDiapose();
                }
            }

            //若所属父视图堆栈已关闭
            if (item.ParentPopup?._isClosed == true)
            {
                CloseAndDisposeItem(item);
            }
            else if (!ce.Cancel)
            {
                var container = this.PopupContainerFromItem(item);
                container.OnCloseAnimation(_p =>
                {
                    CloseAndDisposeItem(item);
                });
            }
            else
            {
                item._isClosing = false;
            }
        }
    }
}
