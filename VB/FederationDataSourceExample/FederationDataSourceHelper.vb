Imports System.ComponentModel
Imports DevExpress.DataAccess.ConnectionParameters
Imports DevExpress.DataAccess.DataFederation
Imports DevExpress.DataAccess.Excel
Imports DevExpress.DataAccess.Json
Imports DevExpress.DataAccess.Sql
Imports DevExpress.DataAccess.Sql.DataApi

Namespace FederationDataSourceExample
	Friend Class FederationDataSourceHelper
		Public Shared Property DataSource() As Object
		Public Shared Sub Create_Select_Query_For_Excel_Source()
			Dim federation As New FederationDataSource()

			Dim source As New Source("excelSource", CreateExcelDataSource("SalesPerson.xlsx", "Data"))

			Dim sourceNode = New SourceNode(source, "Orders")

            Dim query = New SelectNode(sourceNode)
            query.Alias = "excel"
            query.Expressions.Add(New SelectColumnExpression(sourceNode, "OrderID"))
            query.Expressions.Add(New SelectColumnExpression(sourceNode, "OrderDate"))
            query.Expressions.Add(New SelectColumnExpression(sourceNode, "Sales Person"))
            query.Expressions.Add(New SelectColumnExpression(sourceNode, "ProductName"))
            query.Expressions.Add(New SelectColumnExpression(sourceNode, "Extended Price"))
            query.FilterString = "[Orders.CategoryName] = ?cat"
            federation.Queries.Add(query)
            federation.Fill( { New DevExpress.DataAccess.Sql.QueryParameter("cat", GetType(String), "Seafood") })
			Dim result = TryCast(DirectCast(federation, IListSource).GetList(), IResultSet)
			DataSource = result(0)
		End Sub

		Public Shared Sub Join_Sql_And_Excel_Sources()
			Dim federation As New FederationDataSource()

			Dim sourceProducts As New Source("Products", CreateSqlDataSource(), "Products")
			Dim sourceOrderDetail As New Source("OrderDetail", CreateExcelDataSource("SalesPerson.xlsx", "Data"))

			Dim sourceNodeProducts = New SourceNode(sourceProducts, "Products")
			Dim sourceNodeOrderDetail = New SourceNode(sourceOrderDetail, "OrderDetail")

            Dim query = New SelectNode(sourceNodeProducts)
            query.Alias = "ProductsOrderDetail"
            query.SubNodes.Add(New JoinElement With {.Node = sourceNodeOrderDetail,
            .Condition = $"[{sourceNodeProducts.Alias}.ProductName] == [{sourceNodeOrderDetail.Alias}.ProductName]"})

            query.Expressions.Add(New SelectColumnExpression(sourceNodeProducts, "QuantityPerUnit"))
            query.Expressions.Add(New SelectColumnExpression(sourceNodeOrderDetail, "OrderID"))
            query.Expressions.Add(New SelectColumnExpression(sourceNodeOrderDetail, "OrderDate"))
            query.Expressions.Add(New SelectColumnExpression(sourceNodeOrderDetail, "Quantity"))
            query.Expressions.Add(New SelectColumnExpression(sourceNodeOrderDetail, "UnitPrice"))
            federation.Queries.Add(query)
            federation.Fill()
			Dim result = TryCast(DirectCast(federation, IListSource).GetList(), IResultSet)
			DataSource = result(0)
		End Sub

		Public Shared Sub Two_Queries_Created_With_Fluent_Interface()
			Dim federation As New FederationDataSource()

			Dim sourceProducts As New Source("Products", CreateSqlDataSource(), "Products")
			Dim sourceOrderDetail As New Source("OrderDetail", CreateExcelDataSource("SalesPerson.xlsx", "Data"))
			Dim sourceHeader As New Source("OrderHeader", CreateExcelDataSource("OrderHeaders.xlsx", "OrderHeader"))


			Dim query1 As SelectNode = sourceProducts.From().Select("ProductName", "QuantityPerUnit", "UnitsInStock").Join(sourceOrderDetail, "[Products.ProductName] = [OrderDetail.ProductName]").Select("OrderDate").Build("ProductsOrderDetail")

			Dim query2 As SelectNode = sourceHeader.From().Select("OrderID", "Status", "Description").Join(sourceOrderDetail, "[OrderHeader.OrderID] = [OrderDetail.OrderID]").Select("Quantity", "Extended Price").Build("OrderHeaderOrderDetail")

			federation.Queries.AddRange( { query1, query2 })
			federation.Fill()
			DataSource = federation
		End Sub

		Public Shared Sub Create_Master_Detail_Relation()
			Dim federation As New FederationDataSource()

			Dim ordersList = New List(Of Order) From {
				New Order With {.OrderID = 10273, .Status = "Paid", .Description = "Smooth"},
				New Order With {.OrderID = 10273, .Status = "Paid", .Description = "Bright"},
				New Order With {.OrderID = 10274, .Status = "Paid", .Description = "Crisp"},
				New Order With {.OrderID = 10276, .Status = "Paid", .Description = "Excellent"},
				New Order With {.OrderID = 10278, .Status = "Paid", .Description = "Poor"}
			}

			Dim sourceOrders As New Source("Orders", ordersList)
			Dim sourceExcel As New Source("OrderDetail", CreateExcelDataSource("SalesPerson.xlsx", "Data"))

			Dim sourceNodeOrders = New SourceNode(sourceOrders, "Orders")
			Dim sourceNodeExcel = New SourceNode(sourceExcel, "OrderDetail")

            Dim queryOrders = New SelectNode(sourceNodeOrders)
            queryOrders.Alias = "Orders"
            queryOrders.Expressions.Add(New SelectColumnExpression(sourceNodeOrders, "OrderID"))
            queryOrders.Expressions.Add(New SelectColumnExpression(sourceNodeOrders, "Status"))
            queryOrders.Expressions.Add(New SelectColumnExpression(sourceNodeOrders, "Description"))
            federation.Queries.Add(queryOrders)

            Dim queryExcel = New SelectNode(sourceNodeExcel)
            queryExcel.Alias = "OrderDetail"
            queryExcel.Expressions.Add(New SelectColumnExpression(sourceNodeExcel, "OrderID"))
            queryExcel.Expressions.Add(New SelectColumnExpression(sourceNodeExcel, "OrderDate"))
            queryExcel.Expressions.Add(New SelectColumnExpression(sourceNodeExcel, "Sales Person"))
            queryExcel.Expressions.Add(New SelectColumnExpression(sourceNodeExcel, "ProductName"))
            queryExcel.Expressions.Add(New SelectColumnExpression(sourceNodeExcel, "Extended Price"))
            federation.Queries.Add(queryExcel)

            federation.Relations.Add(New FederationMasterDetailInfo("Orders", "OrderDetail", New FederationRelationColumnInfo() With
                                                                    {.ParentKeyColumn = "OrderID",
                                                                    .NestedKeyColumn = "OrderID",
                                                                    .ConditionOperator = FederationConditionType.Equal}))
            federation.Fill()
            DataSource = federation
		End Sub

		Public Shared Sub Integrate_SQL_Excel_JSON_Data_Sources()
			Dim federation As New FederationDataSource()

			Dim sourceProducts As New Source("Products", CreateSqlDataSource(), "Products")
			Dim sourceOrderDetail As New Source("OrderDetail", CreateExcelDataSource("SalesPerson.xlsx", "Data"))
			Dim sourceHeader As New Source("OrderHeader", CreateExcelDataSource("OrderHeaders.xlsx", "OrderHeader"))
			Dim sourceCustomer As New Source("Customer", CreateJsonDataSource("http://northwind.servicestack.net/customers.json"),"Customers")

            Dim query As SelectNode = sourceHeader.From().
                Select("OrderID", "Status", "Description").
                Join(sourceOrderDetail, "[OrderHeader.OrderID] = [OrderDetail.OrderID]").
                Select("ProductName", "Quantity", "Extended Price").
                Join(sourceCustomer, "[OrderHeader.CustomerID] = [Customer.Id]").
                Select("CompanyName", "ContactName", "City", "Country").
                Join(sourceProducts, "[OrderDetail.ProductName]=[Products.ProductName]").
                Select("QuantityPerUnit", "CategoryID").Build("OrderHeaderOrderCustomerProducts")

            federation.Queries.Add(query)
			federation.Fill()
			Dim result = TryCast(DirectCast(federation, IListSource).GetList(), IResultSet)
			DataSource = result(0)
		End Sub

		Private Shared Function CreateSqlDataSource() As SqlDataSource
			Dim sqlDataSource = New SqlDataSource(New XmlFileConnectionParameters("Products.xml"))
			sqlDataSource.Queries.Add(SelectQueryFluentBuilder.AddTable("Products").SelectColumns("ProductName", "QuantityPerUnit", "CategoryID", "UnitsInStock").Build("Products"))
			Return sqlDataSource
		End Function
		Private Shared Function CreateExcelDataSource(ByVal fileName As String, ByVal worksheetName As String) As ExcelDataSource
            Dim excelDataSource = New ExcelDataSource With {
                .FileName = fileName, .SourceOptions = New ExcelSourceOptions With {
                    .SkipEmptyRows = False,
                    .SkipHiddenColumns = False,
                    .SkipHiddenRows = False,
                    .ImportSettings = New ExcelWorksheetSettings With {.WorksheetName = worksheetName}
                }
            }
            Return excelDataSource
		End Function
		Private Shared Function CreateJsonDataSource(ByVal urlString As String) As JsonDataSource
			Dim jsonDataSource = New JsonDataSource()
			jsonDataSource.JsonSource = New UriJsonSource(New Uri(urlString))
			jsonDataSource.Fill()
			Return jsonDataSource
		End Function
	End Class

	Public Class Order
		Public Property OrderID() As Integer
		Public Property Status() As String
		Public Property Description() As String
	End Class
End Namespace
