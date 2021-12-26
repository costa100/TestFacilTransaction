module Tests

open System
open Microsoft.Data.SqlClient
open Xunit

open DbGen

// change the connection string to point to your db
let connectionStr = "Server=localhost; Database=Test; Trusted_Connection=true; TrustServerCertificate=True"


[<Fact>]
let ``Handling temp tables within a sql transaction should work`` () =
    use cn = new SqlConnection(connectionStr)
    cn.Open()
    use tran = cn.BeginTransaction()
    
    let rows = seq { for i in 1 .. 100 do Scripts.SelectFromTempTable.TempTable.create(i, Some $"Item {i}") }
    
    let result =
        Scripts.SelectFromTempTable
            .WithConnection(cn)
            .WithParameters(rows)
            .Execute()
    
    result.ToArray() |> Array.iter (printfn "%A")
    
    tran.Commit()
    
    ()
    
    