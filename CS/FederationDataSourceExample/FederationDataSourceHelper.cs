using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.DataFederation;
using DevExpress.DataAccess.Excel;
using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.Sql;
using DevExpress.DataAccess.Sql.DataApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FederationDataSourceExample
{
    class FederationDataSourceHelper
    {
        static public object DataSource { get; set; }
        static public void Create_Select_Query_For_Excel_Source()
        {
            FederationDataSource federation = new FederationDataSource();

            Source source = new Source("excelSource", CreateExcelDataSource("SalesPerson.xlsx", "Data"));

            var sourceNode = new SourceNode(source, "Orders");

            var query = new SelectNode(sourceNode)
            {
                Alias = "excel",
                Expressions = {
                        new SelectColumnExpression(sourceNode, "OrderID"),
                        new SelectColumnExpression(sourceNode, "OrderDate"),
                        new SelectColumnExpression(sourceNode, "Sales Person"),
                        new SelectColumnExpression(sourceNode, "ProductName"),
                        new SelectColumnExpression(sourceNode, "Extended Price")
                    },
                FilterString = "[Orders.CategoryName] = ?cat",
            };
            federation.Queries.Add(query);
            federation.Fill(new[] { new DevExpress.DataAccess.Sql.QueryParameter("cat", typeof(string), "Seafood") });
            var result = ((IListSource)federation).GetList() as IResultSet;
            DataSource = result[0];
        }

        static public void Join_Sql_And_Excel_Sources()
        {
            FederationDataSource federation = new FederationDataSource();

            Source sourceProducts = new Source("Products", CreateSqlDataSource(), "Products");
            Source sourceOrderDetail = new Source("OrderDetail", CreateExcelDataSource("SalesPerson.xlsx", "Data"));

            var sourceNodeProducts = new SourceNode(sourceProducts, "Products");
            var sourceNodeOrderDetail = new SourceNode(sourceOrderDetail, "OrderDetail");

            var query = new SelectNode(sourceNodeProducts)
            {
                Alias = "ProductsOrderDetail",
                SubNodes = {
                        new JoinElement {
                            Node = sourceNodeOrderDetail,
                            Condition = $"[{sourceNodeProducts.Alias}.ProductName] == [{sourceNodeOrderDetail.Alias}.ProductName]"
                        }
                    },
                Expressions = {
                    new SelectColumnExpression(sourceNodeProducts, "ProductName"),
                    new SelectColumnExpression(sourceNodeProducts, "QuantityPerUnit"),
                    new SelectColumnExpression(sourceNodeOrderDetail, "OrderID"),
                    new SelectColumnExpression(sourceNodeOrderDetail, "OrderDate"),
                    new SelectColumnExpression(sourceNodeOrderDetail, "Quantity"),
                    new SelectColumnExpression(sourceNodeOrderDetail, "UnitPrice")
                    }
            };
            federation.Queries.Add(query);
            federation.Fill();
            var result = ((IListSource)federation).GetList() as IResultSet;
            DataSource = result[0];
        }

        static public void Two_Queries_Created_With_Fluent_Interface()
        {
            FederationDataSource federation = new FederationDataSource();

            Source sourceProducts = new Source("Products", CreateSqlDataSource(), "Products");
            Source sourceOrderDetail = new Source("OrderDetail", CreateExcelDataSource("SalesPerson.xlsx", "Data"));
            Source sourceHeader = new Source("OrderHeader", CreateExcelDataSource("OrderHeaders.xlsx", "OrderHeader"));


            SelectNode query1 = sourceProducts.From().Select("ProductName", "QuantityPerUnit", "UnitsInStock")
                .Join(sourceOrderDetail, "[Products.ProductName] = [OrderDetail.ProductName]")
                .Select("OrderDate")
                .Build("ProductsOrderDetail");

            SelectNode query2 = sourceHeader.From().Select("OrderID", "Status", "Description")
                .Join(sourceOrderDetail, "[OrderHeader.OrderID] = [OrderDetail.OrderID]")
                .Select("Quantity", "Extended Price")
                .Build("OrderHeaderOrderDetail");

            federation.Queries.AddRange(new[] { query1, query2 });
            federation.Fill();
            DataSource = federation;
        }

        static public void Create_Master_Detail_Relation()
        {
            FederationDataSource federation = new FederationDataSource();

            var ordersList = new List<Order> {
                new Order { OrderID = 10273, Status = "Paid", Description = "Smooth" },
                new Order { OrderID = 10273, Status = "Paid", Description = "Bright" },
                new Order { OrderID = 10274, Status = "Paid", Description = "Crisp" },
                new Order { OrderID = 10276, Status = "Paid", Description = "Excellent" },
                new Order { OrderID = 10278, Status = "Paid", Description = "Poor" }
            };

            Source sourceOrders = new Source("Orders", ordersList);
            Source sourceExcel = new Source("OrderDetail", CreateExcelDataSource("SalesPerson.xlsx", "Data"));

            var sourceNodeOrders = new SourceNode(sourceOrders, "Orders");
            var sourceNodeExcel = new SourceNode(sourceExcel, "OrderDetail");

            var queryOrders = new SelectNode(sourceNodeOrders)
            {
                Alias = "Orders",
                Expressions = {
                    new SelectColumnExpression(sourceNodeOrders, "OrderID"),
                    new SelectColumnExpression(sourceNodeOrders, "Status"),
                    new SelectColumnExpression(sourceNodeOrders, "Description")
                    }
            };
            federation.Queries.Add(queryOrders);

            var queryExcel = new SelectNode(sourceNodeExcel)
            {
                Alias = "OrderDetail",
                Expressions = {
                        new SelectColumnExpression(sourceNodeExcel, "OrderID"),
                        new SelectColumnExpression(sourceNodeExcel, "OrderDate"),
                        new SelectColumnExpression(sourceNodeExcel, "Sales Person"),
                        new SelectColumnExpression(sourceNodeExcel, "ProductName"),
                        new SelectColumnExpression(sourceNodeExcel, "Extended Price")
                    }
            };
            federation.Queries.Add(queryExcel);

            federation.Relations.Add(new FederationMasterDetailInfo() {
                MasterQueryName = "Orders",
                DetailQueryName = "OrderDetail",
                KeyColumns = {
                    new FederationRelationColumnInfo(){
                        ParentKeyColumn="OrderID",
                        NestedKeyColumn ="OrderID",
                        ConditionOperator= FederationConditionType.Equal
                        }
                    }
            }
            );

            federation.Fill();
            DataSource = federation;
        }

        static public void Integrate_SQL_Excel_JSON_Data_Sources()
        {
            FederationDataSource federation = new FederationDataSource();

            Source sourceProducts = new Source("Products", CreateSqlDataSource(), "Products");
            Source sourceOrderDetail = new Source("OrderDetail", CreateExcelDataSource("SalesPerson.xlsx", "Data"));
            Source sourceHeader = new Source("OrderHeader", CreateExcelDataSource("OrderHeaders.xlsx", "OrderHeader"));
            Source sourceCustomer = new Source("Customer", CreateJsonDataSource("http://northwind.servicestack.net/customers.json"),"Customers");

            SelectNode query = sourceHeader.From().Select("OrderID","Status", "Description")
                .Join(sourceOrderDetail, "[OrderHeader.OrderID] = [OrderDetail.OrderID]")
                .Select("ProductName","Quantity", "Extended Price")
                .Join(sourceCustomer, "[OrderHeader.CustomerID] = [Customer.Id]")
                .Select("CompanyName", "ContactName", "City", "Country")
                .Join(sourceProducts, "[OrderDetail.ProductName]=[Products.ProductName]")
                .Select("QuantityPerUnit", "CategoryID")
                .Build("OrderHeaderOrderCustomerProducts");

            federation.Queries.Add(query);
            federation.Fill();
            var result = ((IListSource)federation).GetList() as IResultSet;
            DataSource = result[0];
        }

        static SqlDataSource CreateSqlDataSource()
        {
            var sqlDataSource = new SqlDataSource(new XmlFileConnectionParameters("Products.xml"));
            sqlDataSource.Queries.Add(SelectQueryFluentBuilder.AddTable("Products").SelectColumns("ProductName", "QuantityPerUnit", "CategoryID", "UnitsInStock").Build("Products"));
            return sqlDataSource;
        }
        static ExcelDataSource CreateExcelDataSource(string fileName, string worksheetName)
        {
            var excelDataSource = new ExcelDataSource
            {
                FileName = fileName,
                SourceOptions = new ExcelSourceOptions
                {
                    SkipEmptyRows = false,
                    SkipHiddenColumns = false,
                    SkipHiddenRows = false,
                    ImportSettings = new ExcelWorksheetSettings { WorksheetName = worksheetName }
                }
            };
            return excelDataSource;
        }
        static  JsonDataSource CreateJsonDataSource(string urlString)
        {
            var jsonDataSource = new JsonDataSource();
            jsonDataSource.JsonSource = new UriJsonSource(new Uri(urlString));
            jsonDataSource.Fill();
            return jsonDataSource;
        }
    }

    public class Order
    {
        public int OrderID { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }
}
