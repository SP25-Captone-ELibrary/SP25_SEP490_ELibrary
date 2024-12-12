using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Utils;

public static class CsvUtils
{
    public static List<T> ReadCsvOrExcel<T>(IFormFile file, CsvConfiguration config, string? encodingType)
        where T : class, new()
    {
        var records = new List<T>();
        
        if (file.FileName.EndsWith(".csv"))
        {
            // Handle CSV file
            Encoding encoding = encodingType?.ToUpper() switch
            {
                "ASCII" => Encoding.ASCII,
                "UTF-8" => Encoding.UTF8,
                _ => Encoding.Default
            };
            
            using (var reader = new StreamReader(file.OpenReadStream(), encoding))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    var record = csv.GetRecord<T>();
                    records.Add(record);
                }
            }
        }
        else if (file.FileName.EndsWith(".xlsx"))
        {
            // Handle Excel file
            using (var stream = file.OpenReadStream())
            using (var package = new OfficeOpenXml.ExcelPackage(stream))
            {
                // Get the first worksheet
                var worksheet = package.Workbook.Worksheets[0];
                // Get number of rows
                var rows = worksheet.Dimension.Rows; 
                // Get number of columns
                var cols = worksheet.Dimension.Columns;

                // First row as headers
                var headerRow = worksheet.Cells[1, 1, 1, cols]; 
                // Read header values
                var headers = headerRow.Select(cell => cell.Text).ToList(); 

                // Skip header row, start from 2nd row
                for (int rowIndex = 2; rowIndex <= rows; rowIndex++) 
                {
                    // Get current row's cells
                    var row = worksheet.Cells[rowIndex, 1, rowIndex, cols];
                    var record = new T();

                    foreach (var prop in typeof(T).GetProperties())
                    {
                        if (!headers.Contains(prop.Name)) continue;

                        // Get column index 
                        var columnIndex = headers.IndexOf(prop.Name) + 1; 
                        // Get the cell's value as text
                        var cellValue = worksheet.Cells[rowIndex, columnIndex].Text; 

                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            // Try to convert and set the value to the property
                            prop.SetValue(record, Convert.ChangeType(cellValue, prop.PropertyType));
                        }
                    }

                    records.Add(record);
                }
            }
        }
        else
        {
            throw new NotSupportedException("Only CSV and Excel (.xlsx) files are supported.");
        }

        return records;
    }
}