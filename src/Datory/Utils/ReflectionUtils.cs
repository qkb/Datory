﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Datory.Annotations;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("Datory.Data.Tests")]

namespace Datory.Utils
{
    internal static class ReflectionUtils
    {
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();

        internal static List<PropertyInfo> GetTypeProperties(Type type)
        {
            if (TypeProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> pis))
            {
                return pis.ToList();
            }

            var properties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).ToArray();

            TypeProperties[type.TypeHandle] = properties;
            return properties.ToList();
        }

        private static PropertyInfo GetTypeProperty(Type type, string propertyName)
        {
            var propertyInfoList = GetTypeProperties(type);
            return propertyInfoList.FirstOrDefault(x => x.Name == propertyName);
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> TypeTableName = new ConcurrentDictionary<RuntimeTypeHandle, string>();

        public static string GetTableName(Type type)
        {
            if (TypeTableName.TryGetValue(type.TypeHandle, out string name)) return name;

            var attribute = (DataTableAttribute)Attribute.GetCustomAttribute(type, typeof(DataTableAttribute));
            name = attribute == null ? string.Empty : attribute.Name;

            TypeTableName[type.TypeHandle] = name;
            return name;
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<TableColumn>> TableColumns = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<TableColumn>>();

        public static List<TableColumn> GetTableColumns(Type type)
        {
            if (TableColumns.TryGetValue(type.TypeHandle, out var tc))
            {
                return tc.ToList();
            }

            var tableColumns = GetTableColumnsByReflection(type);

            TableColumns[type.TypeHandle] = tableColumns;
            return tableColumns;
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<string>> PropertyNames = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<string>>();

        public static List<string> GetPropertyNames(Type type)
        {
            if (PropertyNames.TryGetValue(type.TypeHandle, out var tc))
            {
                return tc.ToList();
            }

            var names = GetTypeProperties(type).Select(x => x.Name).ToList();

            PropertyNames[type.TypeHandle] = names;
            return names;
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<string>> ColumnNames = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<string>>();

        public static List<string> GetColumnNames(Type type)
        {
            if (ColumnNames.TryGetValue(type.TypeHandle, out var tc))
            {
                return tc.ToList();
            }

            var names = GetTableColumns(type).Select(x => x.AttributeName).ToList();

            ColumnNames[type.TypeHandle] = names;
            return names;
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> TableExtendColumnName = new ConcurrentDictionary<RuntimeTypeHandle, string>();

        public static string GetTableExtendColumnName(Type type)
        {
            if (TableExtendColumnName.TryGetValue(type.TypeHandle, out var tc))
            {
                return tc;
            }

            var columnName =
                GetTableColumns(type).Where(x => x.IsExtend).Select(x => x.AttributeName).FirstOrDefault() ??
                string.Empty;

            TableExtendColumnName[type.TypeHandle] = columnName;
            return columnName;
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<string>> DataIgnoreNames = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<string>>();

        public static List<string> GetDataIgnoreNames(Type type)
        {
            if (DataIgnoreNames.TryGetValue(type.TypeHandle, out var tc))
            {
                return tc.ToList();
            }

            var ignores = new List<string>();

            var properties = GetTypeProperties(type);

            foreach (var propertyInfo in properties)
            {
                var attribute = propertyInfo.GetCustomAttribute<DataIgnoreAttribute>(true);
                if (attribute == null) continue;

                ignores.Add(propertyInfo.Name);
            }

            DataIgnoreNames[type.TypeHandle] = ignores;
            return ignores;
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<string>> JsonIgnoreNames = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<string>>();

        public static List<string> GetJsonIgnoreNames(Type type)
        {
            if (JsonIgnoreNames.TryGetValue(type.TypeHandle, out var tc))
            {
                return tc.ToList();
            }

            var ignores = new List<string>();

            var properties = GetTypeProperties(type);

            foreach (var propertyInfo in properties)
            {
                var attribute = propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>(true);
                if (attribute == null) continue;

                ignores.Add(propertyInfo.Name);
            }

            JsonIgnoreNames[type.TypeHandle] = ignores;
            return ignores;
        }

        private static List<TableColumn> GetTableColumnsByReflection(Type type)
        {
            var entityColumns = new List<TableColumn>();
            var tableColumns = new List<TableColumn>();

            var properties = GetTypeProperties(type);

            foreach (var propertyInfo in properties)
            {
                var attribute = propertyInfo.GetCustomAttribute<DataColumnAttribute>(true);
                if (attribute == null) continue;

                var dataType = DataType.VarChar;
                var dataLength = 0;
                var dataExtend = false;

                var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

                if (propertyType == typeof(string) || propertyType == typeof(char) || propertyType == typeof(Enum))
                {
                    if (attribute.Text)
                    {
                        dataType = DataType.Text;
                        dataExtend = attribute.Extend;
                    }
                    else
                    {
                        dataType = DataType.VarChar;
                        dataLength = attribute.Length;
                        if (dataLength <= 0)
                        {
                            dataLength = DbUtils.VarCharDefaultLength;
                        }
                    }
                }
                else if (propertyType == typeof(int))
                {
                    dataType = DataType.Integer;
                }
                else if (propertyType == typeof(bool))
                {
                    dataType = DataType.Boolean;
                }
                else if (propertyType == typeof(DateTimeOffset) || propertyType == typeof(DateTime))
                {
                    dataType = DataType.DateTime;
                }
                else if (propertyType == typeof(double) || propertyType == typeof(decimal))
                {
                    dataType = DataType.Decimal;
                }

                var tableColumn = new TableColumn
                {
                    AttributeName = propertyInfo.Name,
                    DataType = dataType,
                    DataLength = dataLength,
                    IsExtend = dataExtend
                };

                if (Utilities.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.Id)) ||
                    Utilities.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.Guid)) ||
                    Utilities.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.CreatedDate)) ||
                    Utilities.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.LastModifiedDate)))
                {
                    entityColumns.Add(tableColumn);
                }
                else
                {
                    tableColumns.Add(tableColumn);
                }
            }

            var columns = new List<TableColumn>();
            columns.AddRange(entityColumns);
            columns.AddRange(tableColumns);

            return columns;
        }

        public static object GetValue(object obj, string propertyName)
        {
            var property = GetTypeProperty(obj.GetType(), propertyName);
            if (property != null && property.CanRead)
            {
                var val = property.GetValue(obj, null);

                if (property.PropertyType.IsEnum)
                {
                    try
                    {
                        return Enum.Parse(property.PropertyType, val.ToString(), true);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                return val;
            }

            return null;
        }

        public static void SetValue(object obj, string propertyName, object val)
        {
            var property = GetTypeProperty(obj.GetType(), propertyName);

            if (property != null && property.CanWrite)
            {
                if (val == null)
                {
                    property.SetValue(obj, null, null);
                }
                else if (property.PropertyType.IsEnum)
                {
                    try
                    {
                        property.SetValue(obj, Enum.Parse(property.PropertyType, val.ToString(), true), null);
                    }
                    catch
                    {
                        property.SetValue(obj, ChangeType(val, property.PropertyType), null);
                    }
                }
                else
                {
                    property.SetValue(obj, ChangeType(val, property.PropertyType), null);
                }
            }
        }

        private static object ChangeType(object value, Type conversionType)
        {
            try
            {
                return Convert.ChangeType(value, conversionType);
            }
            catch
            {
                return GetDefault(conversionType);
            }
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
