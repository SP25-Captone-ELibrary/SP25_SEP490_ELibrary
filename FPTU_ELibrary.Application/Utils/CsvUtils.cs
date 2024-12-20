using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using FPTU_ELibrary.Domain.Common.Enums;
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
    
    public static byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1") where T : class
    {
        // Create a new Excel package
        using (var package = new OfficeOpenXml.ExcelPackage())
        {
            // Add a worksheet to the package
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // Get properties of T
            var properties = typeof(T).GetProperties();

            // Write the headers
            for (int col = 0; col < properties.Length; col++)
            {
                worksheet.Cells[1, col + 1].Value = properties[col].Name;
            }

            // Write data rows
            int rowIndex = 2; // Start from the second row 
            foreach (var item in data)
            {
                for (int col = 0; col < properties.Length; col++)
                {
                    // Get the value of the property and write it to the cell
                    var value = properties[col].GetValue(item);
                    worksheet.Cells[rowIndex, col + 1].Value = value;
                }

                rowIndex++;
            }

            // Turn on auto-fit columns 
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Return the Excel file as a byte array
            return package.GetAsByteArray();
        }
    }

    public static (List<T> handledRecords, string msg) HandleDuplicates<T>(
        List<T> records, 
        Dictionary<int, List<int>> duplicateIndexes,
        DuplicateHandle duplicateHandle,
        SystemLanguage? lang)
    {
        // Initialize fields
        var msg = string.Empty;
        var isEng = lang == SystemLanguage.English;
        
        // Handling based on DuplicateHandle
		switch (duplicateHandle)
		{
			case DuplicateHandle.Allow:
				// Allow all duplicate items 
				break;
			case DuplicateHandle.Replace:
				// Initialize to remove items						
				var objectsToRemove = new HashSet<T>();
				foreach (var duplicateIdx in duplicateIndexes.Keys)
				{
					// Get all other duplicate elements
					var otherDuplicateElements = duplicateIndexes[duplicateIdx].Select(idx => records[idx]);
					
					// Remove duplicate
					foreach (var obj in otherDuplicateElements)
					{
						objectsToRemove.Add(obj);
					}
				}

				// Remove all marked duplicates
				records.RemoveAll(record => objectsToRemove.Contains(record));
				
                msg = isEng
					? $"{duplicateIndexes.Keys.Count} data have been replaced"
					: $"{duplicateIndexes.Keys.Count} đã bị lượt bỏ";
				break;
			case DuplicateHandle.Skip:
				// Initialize object to skip
				var objectsToSkip = new HashSet<T>();
				foreach (var duplicateIdx in duplicateIndexes.Keys)
                {
                    // Get all duplicate elements include the first one
                    var allDuplicateObjects = new List<T> { records[duplicateIdx] };
                    allDuplicateObjects.AddRange(duplicateIndexes[duplicateIdx].Select(idx => records[idx]));
                
                    foreach (var obj in allDuplicateObjects)
                    {
                        objectsToSkip.Add(obj);
                    }
                }
				
				// Remove all marked objects
				records.RemoveAll(record => objectsToSkip.Contains(record));
				
                // Update the additional message
				msg = isEng
					? $"{objectsToSkip.Count} data have been replaced"
					: $"{objectsToSkip.Count} đã bị lượt bỏ";
				break;
		}

        return (records, msg);
    }
}