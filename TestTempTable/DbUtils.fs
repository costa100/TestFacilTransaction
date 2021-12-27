module DbUtils

open System.Data
open Facil.Runtime.CSharp
open Microsoft.Data.SqlClient

type BulkCopyTempDataLoader(fieldCount: int, data: seq<obj array>) =

    let enumerator = data.GetEnumerator()

    interface IDataReader  with
      member x.Read() = enumerator.MoveNext()
      member x.FieldCount with get() = fieldCount
      member x.GetValue i = enumerator.Current.[i]
      
      member x.Close() = failwith "not implemented"
      member x.Dispose() = ()
      member x.GetBoolean i = failwith "not implemented"
      member x.GetByte i = failwith "not implemented"
      member x.GetBytes(i, fieldOffset, buffer, bufferoffset, length) = failwith "not implemented"
      member x.GetChar i = failwith "not implemented"
      member x.GetChars(i, fieldOffset, buffer, bufferoffset, length) = failwith "not implemented"
      member x.GetData i = failwith "not implemented"
      member x.GetDataTypeName i = failwith "not implemented"
      member x.GetDateTime i = failwith "not implemented"
      member x.GetDecimal i = failwith "not implemented"
      member x.GetDouble i = failwith "not implemented"
      member x.GetFieldType i = failwith "not implemented"
      member x.GetFloat i = failwith "not implemented"
      member x.GetGuid i = failwith "not implemented"
      member x.GetInt16 i = failwith "not implemented"
      member x.GetInt32 i = failwith "not implemented"
      member x.GetInt64 i = failwith "not implemented"
      member x.GetName i = failwith "not implemented"
      member x.GetOrdinal name = failwith "not implemented"
      member x.GetSchemaTable() = failwith "not implemented"
      member x.GetString i = failwith "not implemented"
      
      member x.GetValues values = failwith "not implemented"
      member x.IsDBNull i = failwith "not implemented"
      member x.NextResult() = failwith "not implemented"
      member x.Depth with get() = 0
      member x.IsClosed with get() = failwith "not implemented"
      member x.RecordsAffected with get() = failwith "not implemented"
      member x.Item
          with get (name: string) : obj = failwith "not implemented"
      member x.Item
          with get (i: int) : obj = failwith "not implemented"

let bulkCopyData (cn: SqlConnection) (tran: SqlTransaction) (configCmd: SqlCommand -> unit) (tempTableData: #seq<TempTableData>) =
    tempTableData
    |> Seq.iter (fun data ->
      
      use cmd = cn.CreateCommand()
      configCmd cmd
      
      cmd.CommandText <- data.Definition
      cmd.ExecuteNonQuery() |> ignore
      
      use bulkCopy = new SqlBulkCopy (cn, SqlBulkCopyOptions.Default, tran)
      bulkCopy.DestinationTableName <- data.DestinationTableName
      bulkCopy.BatchSize <- 10000
      
      use reader = new BulkCopyTempDataLoader(data.NumFields, data.Data)
      bulkCopy.WriteToServer(reader)
      
    )            