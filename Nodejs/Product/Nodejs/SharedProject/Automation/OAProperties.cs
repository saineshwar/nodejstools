﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudioTools.Project.Automation
{
    /// <summary>
    /// Contains all of the properties of a given object that are contained in a generic collection of properties.
    /// </summary>
    [ComVisible(true)]
    public class OAProperties : EnvDTE.Properties
    {
        private NodeProperties target;
        private Dictionary<string, EnvDTE.Property> properties = new Dictionary<string, EnvDTE.Property>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public OAProperties(NodeProperties target)
        {
            Utilities.ArgumentNotNull("target", target);

            this.target = target;
            this.AddPropertiesFromType(target.GetType());
        }

        /// <summary>
        /// Defines the NodeProperties object that contains the defines the properties.
        /// </summary>
        public NodeProperties Target => this.target;

        #region EnvDTE.Properties

        /// <summary>
        /// Microsoft Internal Use Only.
        /// </summary>
        public virtual object Application => null;
        /// <summary>
        /// Gets a value indicating the number of objects in the collection.
        /// </summary>
        public int Count => this.properties.Count;
        /// <summary>
        /// Gets the top-level extensibility object.
        /// </summary>
        public virtual EnvDTE.DTE DTE
        {
            get
            {
                if (this.target.HierarchyNode == null || this.target.HierarchyNode.ProjectMgr == null || this.target.HierarchyNode.ProjectMgr.IsClosed ||
                    this.target.HierarchyNode.ProjectMgr.Site == null)
                {
                    throw new InvalidOperationException();
                }
                return this.target.HierarchyNode.ProjectMgr.Site.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            }
        }

        /// <summary>
        /// Gets an enumeration for items in a collection. 
        /// </summary>
        /// <returns>An enumerator. </returns>
        public IEnumerator GetEnumerator()
        {
            if (this.properties.Count == 0)
            {
                yield return new OANullProperty(this);
            }

            IEnumerator enumerator = this.properties.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        /// <summary>
        /// Returns an indexed member of a Properties collection. 
        /// </summary>
        /// <param name="index">The index at which to return a mamber.</param>
        /// <returns>A Property object.</returns>
        public virtual EnvDTE.Property Item(object index)
        {
            if (index is string)
            {
                var indexAsString = (string)index;
                if (this.properties.ContainsKey(indexAsString))
                {
                    return (EnvDTE.Property)this.properties[indexAsString];
                }
            }
            else if (index is int)
            {
                var realIndex = (int)index - 1;
                if (realIndex >= 0 && realIndex < this.properties.Count)
                {
                    var enumerator = this.properties.Values.GetEnumerator();

                    var i = 0;
                    while (enumerator.MoveNext())
                    {
                        if (i++ == realIndex)
                        {
                            return (EnvDTE.Property)enumerator.Current;
                        }
                    }
                }
            }

            throw new ArgumentException(SR.GetString(SR.InvalidParameter), nameof(index));
        }
        /// <summary>
        /// Gets the immediate parent object of a Properties collection.
        /// </summary>
        public virtual object Parent => null;
        #endregion

        #region methods
        /// <summary>
        /// Add properties to the collection of properties filtering only those properties which are com-visible and AutomationBrowsable
        /// </summary>
        /// <param name="targetType">The type of NodeProperties the we should filter on</param>
        private void AddPropertiesFromType(Type targetType)
        {
            Utilities.ArgumentNotNull("targetType", targetType);

            // If the type is not COM visible, we do not expose any of the properties
            if (!IsComVisible(targetType))
            {
                return;
            }

            // Add all properties being ComVisible and AutomationVisible 
            var propertyInfos = targetType.GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                if (!IsInMap(propertyInfo) && IsComVisible(propertyInfo) && IsAutomationVisible(propertyInfo))
                {
                    AddProperty(propertyInfo);
                }
            }
        }
        #endregion

        #region virtual methods
        /// <summary>
        /// Creates a new OAProperty object and adds it to the current list of properties
        /// </summary>
        /// <param name="propertyInfo">The property to be associated with an OAProperty object</param>
        private void AddProperty(PropertyInfo propertyInfo)
        {
            var attrs = propertyInfo.GetCustomAttributes(typeof(PropertyNameAttribute), false);
            var name = propertyInfo.Name;
            if (attrs.Length > 0)
            {
                name = ((PropertyNameAttribute)attrs[0]).Name;
            }
            this.properties.Add(name, new OAProperty(this, propertyInfo));
        }
        #endregion

        #region helper methods

        private bool IsInMap(PropertyInfo propertyInfo)
        {
            return this.properties.ContainsKey(propertyInfo.Name);
        }

        private static bool IsAutomationVisible(PropertyInfo propertyInfo)
        {
            var customAttributesOnProperty = propertyInfo.GetCustomAttributes(typeof(AutomationBrowsableAttribute), true);

            foreach (AutomationBrowsableAttribute attr in customAttributesOnProperty)
            {
                if (!attr.Browsable)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsComVisible(Type targetType)
        {
            var customAttributesOnProperty = targetType.GetCustomAttributes(typeof(ComVisibleAttribute), true);

            foreach (ComVisibleAttribute attr in customAttributesOnProperty)
            {
                if (!attr.Value)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsComVisible(PropertyInfo propertyInfo)
        {
            var customAttributesOnProperty = propertyInfo.GetCustomAttributes(typeof(ComVisibleAttribute), true);

            foreach (ComVisibleAttribute attr in customAttributesOnProperty)
            {
                if (!attr.Value)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}
