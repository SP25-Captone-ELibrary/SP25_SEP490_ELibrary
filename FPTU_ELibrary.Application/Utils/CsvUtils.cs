using System.Reflection;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
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
        else if (file.FileName.EndsWith(".xlsx") || file.FileName.EndsWith(".xlsm"))
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

                    // Check if the row is empty (all cells are blank)
                    var isRowEmpty = row.All(cell => string.IsNullOrEmpty(cell.Text));
                    if (isRowEmpty)
                    {
                        continue; // Skip this row
                    }
                    
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
    
    public static (List<T> Records, Dictionary<int, string[]> Errors) ReadCsvOrExcelWithErrors<T>(
        IFormFile file, 
        CsvConfiguration config, 
        string? encodingType,
        SystemLanguage? systemLang = SystemLanguage.English)
        where T : class, new()
    {
        // Determine current system lang
        var isEng = systemLang == SystemLanguage.English;
        // Initialize error dictionary
        var errors = new Dictionary<int, string[]>();
        // Initialize list of generic type
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
        else if (file.FileName.EndsWith(".xlsx") || file.FileName.EndsWith(".xlsm"))
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

                    // Check if the row is empty (all cells are blank)
                    var isRowEmpty = row.All(cell => string.IsNullOrEmpty(cell.Text));
                    if (isRowEmpty)
                    {
                        continue; // Skip this row
                    }
                    
                    foreach (var prop in typeof(T).GetProperties())
                    {
                        if (!headers.Contains(prop.Name)) continue;

                        // Get column index 
                        var columnIndex = headers.IndexOf(prop.Name) + 1; 
                        // Get the cell's value as text
                        var cellValue = worksheet.Cells[rowIndex, columnIndex].Text; 

                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            try
                            {
                                SetPropertyValue(prop, record, cellValue, isEng);
                            }
                            catch (Exception innerEx) // Invoke exception while read data
                            {
                                // Add error
                                if (!errors.TryGetValue(rowIndex, out var addedErr))
                                {
                                    errors.Add(rowIndex, [innerEx.Message]);
                                }
                                else
                                {
                                    errors[rowIndex] = errors[rowIndex].Append(innerEx.Message).ToArray();
                                }
                            }
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

        return (records, errors);
    }
    
    public static (List<T> Records, Dictionary<int, string[]> Errors) ReadCsvOrExcelByHeaderIndexWithErrors<T>(
        IFormFile file,
        CsvConfiguration config,
        ExcelProps props,
        string? encodingType,
        SystemLanguage? systemLang = SystemLanguage.English)
        where T : class, new()
    {
        // Determine current system language
        var isEng = systemLang == SystemLanguage.English;
        // Initialize error dictionary
        var errors = new Dictionary<int, string[]>();
        // Initialize list of generic type
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

            int rowIndex = 1; // Start from the header row
            using (var reader = new StreamReader(file.OpenReadStream(), encoding))
            using (var csv = new CsvReader(reader, config))
            {
                rowIndex++; // Increase each data row
                
                csv.Read();
                csv.ReadHeader();
                var headers = csv.HeaderRecord;

                // Map header index to property
                var headerIndexToProperty = MapHeadersToProperties<T>(headers);

                while (csv.Read())
                {
                    var record = new T();
                    foreach (var (index, property) in headerIndexToProperty)
                    {
                        try
                        {
                            var cellValue = csv.GetField(index);
                            if (!string.IsNullOrEmpty(cellValue))
                            {
                                SetPropertyValue(property, record, cellValue, isEng);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!errors.ContainsKey(rowIndex))
                            {
                                errors[rowIndex] = new[] { ex.Message };
                            }
                            else
                            {
                                errors[rowIndex] = errors[rowIndex].Append(ex.Message).ToArray();
                            }
                        }
                    }
                    records.Add(record);
                }
            }
        }
        else if (file.FileName.EndsWith(".xlsx") || file.FileName.EndsWith(".xlsm"))
        {
            // Handle Excel file
            using (var stream = file.OpenReadStream())
            using (var package = new OfficeOpenXml.ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[props.WorkSheetIndex > 0 ? props.WorkSheetIndex : 0];
                var rows = worksheet.Dimension.Rows;
                var cols = worksheet.Dimension.Columns;

                // var headers = worksheet.Cells[1, 1, 1, cols].Select(cell => cell.Text).ToArray();
                var headers = worksheet.Cells[props.FromRow, props.FromCol, props.ToRow, cols].Select(cell => cell.Text).ToArray();
                var headerIndexToProperty = MapHeadersToProperties<T>(headers);

                for (int rowIndex = props.StartRowIndex; rowIndex <= rows; rowIndex++)
                {
                    var record = new T();
                    
                    // Check if the row is empty (all cells are blank)
                    var isRowEmpty = worksheet.Cells[rowIndex, 1, rowIndex, cols]
                        .All(cell => string.IsNullOrEmpty(cell.Text));
                    if (isRowEmpty)
                    {
                        continue; // Skip this row
                    }
                    
                    foreach (var (index, property) in headerIndexToProperty)
                    {
                        try
                        {
                            var cellValue = worksheet.Cells[rowIndex, index].Text;
                            if (!string.IsNullOrEmpty(cellValue))
                            {
                                SetPropertyValue(property, record, cellValue, isEng);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!errors.ContainsKey(rowIndex))
                            {
                                errors[rowIndex] = new[] { ex.Message };
                            }
                            else
                            {
                                errors[rowIndex] = errors[rowIndex].Append(ex.Message).ToArray();
                            }
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

        return (records, errors);
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

    public static byte[] ExportToExcelWithNameAttribute<T>(IEnumerable<T> data, string sheetName = "Sheet1") where T : class
    {
        using (var package = new OfficeOpenXml.ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);
    
            // Get properties of T
            var properties = typeof(T).GetProperties();
    
            // Write headers using the [Name] attribute 
            for (int col = 0; col < properties.Length; col++)
            {
                // Check for the Name attribute
                var nameAttribute = properties[col]
                    .GetCustomAttributes(typeof(NameAttribute), false)
                    .FirstOrDefault() as NameAttribute;
    
                // Use the [Name] attribute if available or use the property name if not found
                var headerName = nameAttribute?.Names.FirstOrDefault() ?? properties[col].Name;
    
                // Write the header to excel sheet
                worksheet.Cells[1, col + 1].Value = headerName;
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
    
    public static (List<T> handledRecords, string msg, int totalInvalidItem) HandleDuplicates<T>(
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
					? $"{objectsToSkip.Count} data have been skip"
					: $"đã bỏ qua {objectsToSkip.Count} dữ liệu";
				break;
		}

        return (records, msg, duplicateIndexes.Keys.Count);
    }
    
    private static Dictionary<int, PropertyInfo> MapHeadersToProperties<T>(string[] headers)
    {
        var headerIndexToProperty = new Dictionary<int, PropertyInfo>();
        var properties = typeof(T).GetProperties();
    
        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i];
            foreach (var prop in properties)
            {
                var attribute = prop.GetCustomAttribute<NameAttribute>();
                if (attribute != null && attribute.Names.Contains(header))
                {
                    headerIndexToProperty[i + 1] = prop; // Excel and Csv indexes are 1-based
                    break;
                }
            }
        }
    
        return headerIndexToProperty;
    }
    
    private static void SetPropertyValue<T>(
        PropertyInfo property,
        T record,
        string value,
        bool isEng)
    {
        try
        {
            Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (string.IsNullOrEmpty(value))
            {
                property.SetValue(record, null);
                return;
            }

            object convertedValue;
            if (targetType == typeof(int))
            {
                convertedValue = int.TryParse(value, out var intValue)
                    ? intValue
                    : throw new FormatException(isEng ? $"Invalid integer value: '{value}'" : $"Số nhập vào không hợp lệ: '{value}'");
            }
            else if (targetType == typeof(decimal))
            {
                convertedValue = decimal.TryParse(value, out var decimalValue)
                    ? decimalValue
                    : throw new FormatException(isEng ? $"Invalid decimal value: '{value}'" : $"Số nhập vào không hợp lệ: '{value}'");
            }
            else if (targetType == typeof(DateTime))
            {
                convertedValue = DateTime.TryParse(value, out var dateValue)
                    ? dateValue
                    : throw new FormatException(isEng ? $"Invalid DateTime value: '{value}'" : $"Ngày không hợp lệ: '{value}'");
            }
            else
            {
                convertedValue = Convert.ChangeType(value, targetType);
            }

            property.SetValue(record, convertedValue);
        }
        catch
        {
            throw new FormatException(isEng
                ? $"Could not convert value '{value}' to type {property.PropertyType.Name}"
                : $"Không thể chuyển đổi giá trị '{value}' sang kiểu {property.PropertyType.Name}");
        }
    }
}

public class ExcelProps
{
    public int FromRow { get; set; }
    public int FromCol { get; set; }
    public int ToRow { get; set; }
    public int StartRowIndex { get; set; }
    public int WorkSheetIndex { get; set; } = 0;
}