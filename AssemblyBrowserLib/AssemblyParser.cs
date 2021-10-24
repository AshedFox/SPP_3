using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace AssemblyBrowserLib
{
    public class AssemblyParser
    {
        public AssemblyTreeNode Parse(string assemblyFilePath)
        {
            var assembly = Assembly.LoadFrom(assemblyFilePath);
            
            var result = new AssemblyTreeNode("Root");

            var types = assembly.GetTypes();
            var typesByNamespaces = types.ToLookup(grouping => grouping.Namespace);
            var extensionMethodsInfos = new List<ExtensionMethodInfo>();

            foreach (var typesByNamespace in typesByNamespaces)
            {
                var namespaceNode = new AssemblyTreeNode($"namespace {typesByNamespace.Key}");

                foreach (var type in typesByNamespace)
                {
                    var typeNode = new AssemblyTreeNode(GetTypeDeclaration(type))
                    {
                        ChildNodes = GetTypeMembers(type, ref extensionMethodsInfos)
                    };

                    namespaceNode.ChildNodes.Add(typeNode);
                }
                
                result.ChildNodes.Add(namespaceNode);
            }

            AddExtensionMethods(ref result, extensionMethodsInfos);
            
            return result;
        }

        private List<AssemblyTreeNode> GetTypeMembers(Type type, ref List<ExtensionMethodInfo> extensionMethodsInfos)
        {
            var members = new List<AssemblyTreeNode>();

            var membersByType = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | 
                                                BindingFlags.NonPublic | BindingFlags.Static | 
                                                BindingFlags.DeclaredOnly);
                    
            foreach (var member in membersByType)
            {
                if (member is FieldInfo fieldInfo)
                {
                    members.Add(new AssemblyTreeNode(GetFieldDeclaration(fieldInfo)));
                }
                else if (member is PropertyInfo propertyInfo)
                {
                    members.Add(new AssemblyTreeNode(GetPropertyDeclaration(propertyInfo)));
                }
                else if (member is MethodInfo methodInfo)
                {
                    var methodSignature = GetMethodSignature(methodInfo);

                    if (methodInfo.IsDefined(typeof(ExtensionAttribute), false))
                    {
                        extensionMethodsInfos.Add(new ExtensionMethodInfo(methodInfo, methodSignature));
                    }
                    else
                    {
                        members.Add(new AssemblyTreeNode(methodSignature));
                    }
                }
            }

            return members;
        }

        private void AddExtensionMethods(ref AssemblyTreeNode rootNode, List<ExtensionMethodInfo> extensionMethodsInfos)
        {
            foreach (var extensionMethodInfo in extensionMethodsInfos)
            {
                var paramType = extensionMethodInfo.MethodInfo.GetParameters()[0].ParameterType;
                
                var namespaceNode = paramType.Namespace is null ? null :
                    rootNode.ChildNodes.FirstOrDefault(node => node.Title.EndsWith(paramType.Namespace));
                
                if (namespaceNode is not null)
                {
                    var typeDeclaration = GetTypeDeclaration(paramType);
                    
                    var typeNode = namespaceNode.ChildNodes.FirstOrDefault(node => node.Title == typeDeclaration);

                    if (typeNode is not null)
                    {
                        typeNode.ChildNodes.Add(new AssemblyTreeNode(extensionMethodInfo.MethodSignature));
                    }
                    else
                    {
                        var extTypeNode = new AssemblyTreeNode(typeDeclaration);
                        extTypeNode.ChildNodes.Add(new AssemblyTreeNode(extensionMethodInfo.MethodSignature));
                        namespaceNode.ChildNodes.Add(extTypeNode);
                    }
                }
                else
                {
                    var extNamespaceNode = new AssemblyTreeNode($"namespace {paramType.Namespace}");
                    var extTypeNode = new AssemblyTreeNode(
                        GetTypeDeclaration(paramType));
                    
                    extTypeNode.ChildNodes.Add(new AssemblyTreeNode(extensionMethodInfo.MethodSignature));
                    extNamespaceNode.ChildNodes.Add(extTypeNode);
                    
                    rootNode.ChildNodes.Add(extNamespaceNode);
                }
            }
        }

        private string GetTypeDeclaration(Type type)
        {
            var builder = new StringBuilder();

            builder.Append($"{GetTypeModifiers(type)}{type.Name}");

            var constraints = string.Empty;
            
            if (type.IsGenericType)
            {
                builder.Append(GetClassOrMethodGenericArguments(type.GetGenericArguments(), out constraints));
            }
            
            var parents = GetTypeParents(type);

            if (parents != string.Empty)
            {
                builder.Append($": {parents} ");
            }
            
            if (constraints != string.Empty)
            {
                builder.Append($" {constraints}");
            }

            return builder.ToString();
        }

        private string GetTypeModifiers(Type type)
        {
            var builder = new StringBuilder();

            if (type.IsPublic)
            {
                builder.Append("public ");
            }
            else if (type.IsNotPublic)
            {
                builder.Append("internal ");
            }
            
            if (type.IsClass)
            {
                if (type.IsAbstract && type.IsSealed)
                {
                    builder.Append("static ");
                }
                else if (type.IsAbstract )
                {
                    builder.Append("abstract ");
                }
                else if (type.IsSealed)
                {
                    builder.Append("sealed ");
                }
                
                builder.Append("class ");
            }
            else if (type.IsEnum)
            {
                builder.Append("enum ");
            }
            else if (type.IsInterface)
            {
                builder.Append("interface ");
            }
            else if (type.IsValueType && !type.IsPrimitive)
            {
                builder.Append("struct ");
            }

            return builder.ToString();
        }

        private string GetTypeParents(Type type)
        {
            var parents = new List<string>();

            if (type.BaseType is not null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType) && 
                type.BaseType != typeof(Enum))
            {
                var parent = type.BaseType.Name;
                
                if (type.BaseType.IsGenericType)
                {
                    parent += GetVariableGenericArguments(type.BaseType.GetGenericArguments());
                }

                parents.Add(parent);
            }

            var interfacesTypes = type.GetInterfaces();

            foreach (var interfaceType in interfacesTypes)
            {
                var parent = interfaceType.Name;

                if (interfaceType.IsGenericType)
                {
                    parent += GetVariableGenericArguments(interfaceType.GetGenericArguments());
                }
                
                parents.Add(parent);
            }

            return string.Join(", ", parents);
        }

        private string GetFieldDeclaration(FieldInfo fieldInfo)
        {
            var builder = new StringBuilder();
            
            builder.Append($"{GetFieldModifiers(fieldInfo)}{fieldInfo.FieldType.Name} ");
            
            if (fieldInfo.FieldType.IsGenericType)
            {
                builder.Append($"{GetVariableGenericArguments(fieldInfo.FieldType.GetGenericArguments())} ");
            }

            builder.Append($"{fieldInfo.Name}");

            return builder.ToString();
        }

        private string GetFieldModifiers(FieldInfo fieldInfo)
        {
            var builder = new StringBuilder();
            
            if (fieldInfo.IsFamily && fieldInfo.IsAssembly)
            {
                builder.Append("protected internal ");
            }
            else if (fieldInfo.IsFamily)
            {
                builder.Append("protected ");
            }
            else if (fieldInfo.IsAssembly)
            {
                builder.Append("internal ");
            }
            else if (fieldInfo.IsFamilyOrAssembly)
            {
                builder.Append("public protected ");
            }
            else if (fieldInfo.IsFamilyAndAssembly)
            {
                builder.Append("private protected ");
            }
            else if (fieldInfo.IsPrivate)
            {
                builder.Append("private ");
            } 
            else if (fieldInfo.IsPublic)
            {
                builder.Append("public ");
            }

            if (fieldInfo.IsLiteral)
            {
                builder.Append("const ");
            }
            else if (fieldInfo.IsInitOnly)
            {
                builder.Append("readonly ");
            } 
            
            if (fieldInfo.IsStatic)
            {
                builder.Append("static ");
            }

            return builder.ToString();
        }
        
        private string GetPropertyDeclaration(PropertyInfo propertyInfo)
        {
            var builder = new StringBuilder();

            var getModifiers = string.Empty;
            var setModifiers = string.Empty;
            
            if (propertyInfo.GetMethod is not null)
            {
                getModifiers = GetMethodModifiers(propertyInfo.GetMethod);
            }

            if (propertyInfo.SetMethod is not null)
            {
                setModifiers = GetMethodModifiers(propertyInfo.SetMethod);
            }

            builder.Append($"{getModifiers}{propertyInfo.PropertyType.Name}");
            if (propertyInfo.PropertyType.IsGenericType)
            {
                builder.Append(GetVariableGenericArguments(propertyInfo.PropertyType.GetGenericArguments()));
            }
            builder.Append($" {propertyInfo.Name} {{ ");
            if (getModifiers != string.Empty)
            {
                builder.Append($"{getModifiers}get; ");
            }

            if (setModifiers != string.Empty)
            {
                builder.Append($"{setModifiers}set; ");
            }

            builder.Append("}");
            
            return builder.ToString();
        }

        private string GetMethodSignature(MethodInfo methodInfo)
        {
            var builder = new StringBuilder();

            builder.Append($"{GetMethodModifiers(methodInfo)}{methodInfo.ReturnType.Name} ");

            if (methodInfo.ReturnType.IsGenericType)
            {
                builder.Append($"{GetVariableGenericArguments(methodInfo.ReturnType.GetGenericArguments())} ");
            }

            builder.Append($"{methodInfo.Name}");

            var constraints = string.Empty;
            
            if (methodInfo.IsGenericMethod)
            {
                builder.Append(GetClassOrMethodGenericArguments(methodInfo.GetGenericArguments(), out constraints));
            }

            builder.Append($"({GetMethodParameters(methodInfo)})");
            
            if (constraints != string.Empty)
            {
                builder.Append(constraints);
            }

            return builder.ToString();
        }

        private string GetMethodModifiers(MethodInfo methodInfo)
        {
            var builder = new StringBuilder();
            
            if (methodInfo.IsFamily)
            {
                builder.Append("protected ");
            }
            else if (methodInfo.IsAssembly)
            {
                builder.Append("internal ");
            }
            else if (methodInfo.IsFamilyOrAssembly)
            {
                builder.Append("protected internal ");
            }
            else if (methodInfo.IsFamilyAndAssembly)
            {
                builder.Append("private protected ");
            }
            else if (methodInfo.IsPrivate)
            {
                builder.Append("private ");
            } 
            else if (methodInfo.IsPublic)
            {
                builder.Append("public ");
            }

            if (methodInfo.IsStatic)
            {
                builder.Append("static ");
            }
            
            if (methodInfo.IsAbstract)
            {
                builder.Append("abstract ");
            }
            else if (methodInfo.IsVirtual)
            {
                builder.Append("virtual ");
            }

            return builder.ToString();
        }
        
        private string GetClassOrMethodGenericArguments(Type[] genericArgumentsTypes, out string constraints)
        {
            var constraintsList = new List<string>();
            var genericArguments = new List<string>();

            foreach (var genericArgumentType in genericArgumentsTypes)
            {
                genericArguments.Add(genericArgumentType.Name);

                if (genericArgumentType.IsGenericParameter)
                {
                    var genericParameterConstraints = GetGenericArgumentConstraints(genericArgumentType);

                    if (genericParameterConstraints != string.Empty)
                    {
                        constraintsList.Add(genericParameterConstraints);
                    }
                }
            }

            constraints = constraintsList.Count > 0 ? $" where {string.Join(",", constraintsList)}" : string.Empty;
            
            return $"<{string.Join(", ", genericArguments)}>";
        }
        
        private string GetGenericArgumentConstraints(Type genericArgument) 
        {
            var constraints = new List<string>();
                                        
            var genericParameterConstraints =
                genericArgument.GetGenericParameterConstraints();
            
            foreach (var typeConstraint in genericParameterConstraints)
            {
                constraints.Add(typeConstraint.Name);
            }
            
            var genericParameterAttributes = genericArgument.GenericParameterAttributes;
            var attributes = genericParameterAttributes &
                             GenericParameterAttributes.SpecialConstraintMask;

            if (attributes != GenericParameterAttributes.None)
            {
                if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                {
                    constraints.Add("class");
                }

                if ((attributes &
                     GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                {
                    constraints.Add("notnull");
                }

                if ((attributes &
                     GenericParameterAttributes.DefaultConstructorConstraint) != 0)
                {
                    constraints.Add("new()");
                }
            }

            return constraints.Count > 0 ? $"{genericArgument.Name}: {string.Join(", ", constraints)}" : string.Empty;
        }
        
        private string GetVariableGenericArguments(Type[] genericArgumentsTypes)
        {
            var genericArguments = new List<string>();

            foreach (var genericArgumentType in genericArgumentsTypes)
            {
                var genericArgument = genericArgumentType.Name;
                if (genericArgumentType.IsGenericType)
                {
                    genericArgument += GetVariableGenericArguments(genericArgumentType.GetGenericArguments());
                }
                
                genericArguments.Add(genericArgument);
            }
            
            return $"<{string.Join(", ", genericArguments)}>";
        }

        private string GetMethodParameters(MethodInfo methodInfo)
        {
            var parameters = new List<string>();

            var parametersInfos = methodInfo.GetParameters();
            if (methodInfo.IsDefined(typeof(ExtensionAttribute), false))
            {
                if (parametersInfos[0].ParameterType.IsGenericType)
                {
                    parameters.Add($"this {parametersInfos[0].ParameterType.Name} " + 
                                   $"{GetVariableGenericArguments(parametersInfos[0].ParameterType.GetGenericArguments())}");
                }
                else
                {
                    parameters.Add($"this {parametersInfos[0].ParameterType.Name} {parametersInfos[0].Name}");
                }

                for (var i = 1; i < parametersInfos.Length; i++)
                {
                    if (parametersInfos[i].ParameterType.IsGenericType)
                    {
                        parameters.Add($"{parametersInfos[i].ParameterType.Name} " + 
                                       $"{GetVariableGenericArguments(parametersInfos[i].ParameterType.GetGenericArguments())}");
                    }
                    else
                    {
                        parameters.Add($"{parametersInfos[i].ParameterType.Name} {parametersInfos[i].Name}");
                    }
                }
            }
            else
            {
                foreach (var parameterInfo in parametersInfos)
                {
                    if (parameterInfo.ParameterType.IsGenericType)
                    {
                        parameters.Add($"{parameterInfo.ParameterType.Name} " + 
                                       $"{GetVariableGenericArguments(parameterInfo.ParameterType.GetGenericArguments())}");
                    }
                    else
                    {
                        parameters.Add($"{parameterInfo.ParameterType.Name} {parameterInfo.Name}");
                    }
                }
            }

            return parameters.Count > 0 ? string.Join(", ", parameters) : string.Empty;
        }
    }
}