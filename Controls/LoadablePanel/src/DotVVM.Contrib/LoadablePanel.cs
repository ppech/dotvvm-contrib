﻿using System;
using System.Collections;
using DotVVM.Framework.Binding;
using DotVVM.Framework.Binding.Expressions;
using DotVVM.Framework.Compilation.Javascript;
using DotVVM.Framework.Controls;
using DotVVM.Framework.Hosting;

namespace DotVVM.Contrib
{
    /// <summary>
    /// Renders a HTML &lt;div&gt; element containing Content that should be loaded in Load PostBack.
    /// </summary>
    [ControlMarkupOptions(AllowContent = false, DefaultContentProperty = nameof(ContentTemplate))]
    public class LoadablePanel : HtmlGenericControl
    {
        public LoadablePanel() : base("div")
        { }

        /// <summary>
        /// Gets or sets a template rendering contents of the control.
        /// </summary>
        [MarkupOptions(MappingMode = MappingMode.InnerElement, Required = true)]
        public ITemplate ContentTemplate
        {
            get { return (ITemplate)GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        public static readonly DotvvmProperty ContentTemplateProperty =
            DotvvmProperty.Register<ITemplate, LoadablePanel>(c => c.ContentTemplate);

        /// <summary>
        /// Gets or sets a template rendering elements indicating PostBack progress.
        /// </summary>
        [MarkupOptions(AllowBinding = false, MappingMode = MappingMode.InnerElement, Required = false)]
        public ITemplate ProgressTemplate
        {
            get { return (ITemplate)GetValue(ProgressTemplateProperty); }
            set { SetValue(ProgressTemplateProperty, value); }
        }

        public static readonly DotvvmProperty ProgressTemplateProperty =
            DotvvmProperty.Register<ITemplate, LoadablePanel>(c => c.ProgressTemplate, null);

        /// <summary>
        /// Gets or sets a function used to load data from server. 
        /// Function is triggered immediately after the page is loaded.
        /// </summary>
        [MarkupOptions(AllowBinding = true, Required = true)]
        public Command Load
        {
            get => (Command)GetValue(LoadProperty);
            set => SetValue(LoadProperty, value);
        }

        public static readonly DotvvmProperty LoadProperty
            = DotvvmProperty.Register<Command, LoadablePanel>(t => t.Load);

        /// <summary>
        /// Gets or sets a flag which control if content of loadable panel is visible during loading. 
        /// Default value is false.
        /// </summary>
        [MarkupOptions(AllowBinding = false, AllowHardCodedValue = true, Required = false)]
        public bool HideUntilLoaded
        {
            get { return (bool)GetValue(HideUntilLoadedProperty); }
            set { SetValue(HideUntilLoadedProperty, value); }
        }

        public static readonly DotvvmProperty HideUntilLoadedProperty =
            DotvvmProperty.Register<bool, LoadablePanel>(t => t.HideUntilLoaded);

        /// <summary>
        /// Gets or sets a collection of values of all loadable panels which are currently loading.
        /// </summary>
        public IEnumerable LoadingItems
        {
            get { return (IEnumerable)GetValue(LoadingItemsProperty); }
            set { SetValue(LoadingItemsProperty, value); }
        }
        public static readonly DotvvmProperty LoadingItemsProperty =
            DotvvmProperty.Register<IEnumerable, LoadablePanel>(t => t.LoadingItems, null);

        protected override void OnInit(IDotvvmRequestContext context)
        {
            if (ProgressTemplate != null)
            {
                Children.Add(GetProgress(context));
            }

            AddContentTemplate(context, this);

            base.OnInit(context);
        }

        protected override void OnPreRender(IDotvvmRequestContext context)
        {
            context.ResourceManager.AddRequiredResource(DotvvmConfigurationExtensions.JavascriptResourceName);

            base.OnPreRender(context);
        }

        private void AddContentTemplate(IDotvvmRequestContext context, HtmlGenericControl modalContentContainer)
        {
            var contentContainer = new HtmlGenericControl("div");

            if (HideUntilLoaded)
            {
                contentContainer.Attributes["style"] = "display:none;";
            }

            modalContentContainer.Children.Add(contentContainer);
            ContentTemplate.BuildContent(context, contentContainer);
        }

        protected override void AddAttributesToRender(IHtmlWriter writer, IDotvvmRequestContext context)
        {
            writer.AddKnockoutDataBind("dotvvm-contrib-LoadablePanel", GetControlBinding());
            base.AddAttributesToRender(writer, context);
        }

        private KnockoutBindingGroup GetControlBinding()
        {
            var binding = new KnockoutBindingGroup();

            var commandBinding = GetCommandBinding(LoadProperty);

            binding.Add("load", GenerateCommandFunction(nameof(LoadProperty), commandBinding, this, useWindowSetTimeout: true));

            if (ProgressTemplate != null)
            {
                binding.Add("progressElement", "true");
            }

            var loadingItemsBinding = GetValueBinding(LoadingItemsProperty);
            if (loadingItemsBinding != null)
            {
                binding.Add("loadingItems", this, LoadingItemsProperty);
            }

            return binding;
        }

        private HtmlGenericControl GetProgress(IDotvvmRequestContext context)
        {
            var div = new HtmlGenericControl("div");
            div.SetDataContextType(this.GetDataContextType());
            ProgressTemplate.BuildContent(context, div);
            return div;
        }

        private string GenerateCommandFunction(string propertyName, ICommandBinding commandBinding, DotvvmControl control, bool useWindowSetTimeout = false, bool isOnChange = false)
        {
            var postBackOptions = new PostbackScriptOptions(useWindowSetTimeout, null, isOnChange, "$element", commandArgs: CodeParameterAssignment.FromIdentifier("ar"));

            return $"(function(){{var ar=[].slice.call(arguments);return {KnockoutHelper.GenerateClientPostBackExpression(propertyName, commandBinding, control, postBackOptions)};}})";
        }
    }
}
