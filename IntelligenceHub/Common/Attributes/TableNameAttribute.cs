﻿namespace IntelligenceHub.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableNameAttribute : Attribute
    {
        public string TableName { get; }

        public TableNameAttribute(string tableName)
        {
            TableName = tableName;
        }
    }
}
