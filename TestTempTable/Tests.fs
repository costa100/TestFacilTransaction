module Tests

open System

open System.Collections.Generic
open Facil.Runtime.CSharp
open Microsoft.Data.SqlClient
open Xunit
open DbUtils

open DbGen

// change the connection string to point to your db
let connectionStr = "Server=localhost; Database=Test; Trusted_Connection=true; TrustServerCertificate=True"

let createAndOpenConnection () =
    let cn = new SqlConnection(connectionStr)
    cn.Open()
    cn
    
let defaultCmdConfig (tran: SqlTransaction) = 
    fun (cmd:SqlCommand) -> 
        cmd.CommandTimeout <- 0
        if tran <> null then
          cmd.Transaction <- tran
          
let createTempData () = seq { for i in 1 .. 10000 do Scripts.SelectFromTempTable.TempTable.create(i, Some $"Item {i}") }

let executeWithinTransaction (action: SqlConnection -> SqlTransaction -> unit) =
    use cn = createAndOpenConnection ()    
    use tran = cn.BeginTransaction()    

    action cn tran    
    tran.Commit()
    
    ()
    

[<Fact>]
let ``Handling temp tables within a sql transaction should work`` () =

    let action cn tran = 
        let rows = createTempData () 
        
        let result =
            Scripts.SelectFromTempTable                
                .WithConnection(cn)
                .ConfigureCommand(defaultCmdConfig tran)
                .WithParameters(rows)
                .Execute()
    
        //result.ToArray() |> Array.iter (printfn "%A")
        ()
    
    executeWithinTransaction action
    
    ()
    
[<Fact>]
let ``Handling temp tables within a sql transaction should work - fix`` () =
    let action cn tran = 
        let tempRows = createTempData () 

        // It would be nice to give the developer the ability to separate the insertion of the data in the temp table
        // from the selection from the temp or temp tables                 
        Scripts.SelectFromTempTable
            .WithConnection(cn)
            .ConfigureCommand(defaultCmdConfig tran)
            .CreateTempTableData(tempRows)
        |> bulkCopyData cn tran (defaultCmdConfig(tran))
            
        // Unfortunately there is no way to read back the data from the temp table using the facil generated code because
        // it tries to insert the temp table data. let's use ado.net
        use cmd = new SqlCommand("select * from #TempTable order by id", cn, tran)
        use sdr = cmd.ExecuteReader()
        let tempRowsInserted = new ResizeArray<Scripts.SelectFromTempTable_Result>()
        while sdr.Read() do
           tempRowsInserted.Add ({id = sdr.GetInt32(0); description = Some (sdr.GetString(1)) }) 
        
        //printfn "%i" tempRowsInserted.Count
        tempRowsInserted |> Seq.iter (printfn "%A")
        Assert.Equal(tempRows |> Seq.length, tempRowsInserted.Count)
        tempRows
            |> Seq.iteri(fun i it ->
                Assert.True(it.Fields.[0] = tempRowsInserted.[i].id
                            && it.Fields.[1] = tempRowsInserted.[i].description.Value))
            

        ()
            
    executeWithinTransaction action
    
    
    
    ()
           