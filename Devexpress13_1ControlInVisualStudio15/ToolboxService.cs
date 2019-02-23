using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;

namespace DevExpress13_1ControlInVisualStudio15
{
    public class ToolboxService
    {
        public static ICollection GetToolboxItems(Assembly a, string newCodeBase, bool throwOnError)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            ArrayList arrayList = new ArrayList();
            AssemblyName assemblyName;
            if (a.GlobalAssemblyCache)
            {
                assemblyName = a.GetName();
                assemblyName.CodeBase = newCodeBase;
            }
            else
            {
                assemblyName = null;
            }

            try
            {
                Type[] types = a.GetTypes();
                foreach (Type type in types)
                {
                    if (typeof(IComponent).IsAssignableFrom(type))
                    {
                        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes) ?? type.GetConstructor(new Type[1] { typeof(IContainer) });

                        if (constructor != null)
                        {
                            try
                            {
                                ToolboxItem toolboxItem = GetToolboxItem(type);
                                if (toolboxItem != null)
                                {
                                    if (assemblyName != null)
                                    {
                                        toolboxItem.AssemblyName = assemblyName;
                                    }
                                    arrayList.Add(toolboxItem);
                                }
                            }
                            catch
                            {
                                if (throwOnError)
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }

                return arrayList;
            }
            catch
            {
                if (throwOnError)
                {
                    throw;
                }
                return arrayList;
            }
        }

        public static ToolboxItem GetToolboxItem(Type toolType, bool nonPublic = false)
        {
            if (toolType == null)
            {
                throw new ArgumentNullException("toolType");
            }

            ToolboxItem toolboxItem = null;

            if ((nonPublic || toolType.IsPublic || toolType.IsNestedPublic) && typeof(IComponent).IsAssignableFrom(toolType) && !toolType.IsAbstract)
            {
                AttributeCollection attributeCollection = TypeDescriptor.GetAttributes(toolType);
                ToolboxItemAttribute toolboxItemAttribute = (ToolboxItemAttribute)attributeCollection[typeof(ToolboxItemAttribute)];
                if (!toolboxItemAttribute.IsDefaultAttribute())
                {
                    Type toolboxItemType = toolboxItemAttribute.ToolboxItemType;
                    if (toolboxItemType != (Type)null)
                    {
                        ConstructorInfo constructor = toolboxItemType.GetConstructor(new Type[1]
                        {
                            typeof(Type)
                        });
                        if (constructor != (ConstructorInfo)null && toolType != (Type)null)
                        {
                            toolboxItem = (ToolboxItem)constructor.Invoke(new object[1]
                            {
                                toolType
                            });
                        }
                        else
                        {
                            constructor = toolboxItemType.GetConstructor(new Type[0]);
                            if (constructor != (ConstructorInfo)null)
                            {
                                toolboxItem = (ToolboxItem)constructor.Invoke(new object[0]);
                                toolboxItem.Initialize(toolType);
                            }
                        }
                    }
                }
                else if (!toolboxItemAttribute.Equals(ToolboxItemAttribute.None) && !toolType.ContainsGenericParameters)
                {
                    toolboxItem = new ToolboxItem(toolType);
                }

                if (toolboxItem != null)
                {
                    toolboxItem.Properties.Add("GroupName", toolType.GetCustomAttributesData()
                        .FirstOrDefault(data => data.AttributeType.FullName == "DevExpress.Utils.ToolboxTabNameAttribute")?.ConstructorArguments[0].Value ?? "Devexpress");
                }

                goto IL_0137;
            }

            if (typeof(ToolboxItem).IsAssignableFrom(toolType))
            {
                toolboxItem = (ToolboxItem)Activator.CreateInstance(toolType, true);
                goto IL_0137;
            }

        IL_0137:

            return toolboxItem;
        }
    }
}
