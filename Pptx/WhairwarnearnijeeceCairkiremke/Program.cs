using System;
using System.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;

using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties;
using NonVisualShapeProperties = DocumentFormat.OpenXml.Presentation.NonVisualShapeProperties;

using var document = SpreadsheetDocument.Open("Test.xlsx",isEditable:true);
CopyExcelSheet(document, "Sheet1", "Sheet2", 0);


WorksheetPart CopyExcelSheet(
    SpreadsheetDocument spreadsheetDocument,
    string sourceSheetName,
    string newSheetName,
    int insertPosition = -1)
{
    var workbookPart = spreadsheetDocument.WorkbookPart;

    // ����Դ������
    var sourceSheet = workbookPart.Workbook.Sheets.Elements<Sheet>()
        .FirstOrDefault(s => s.Name == sourceSheetName)
        ?? throw new Exception($"������ '{sourceSheetName}' δ�ҵ�.");

    

    var worksheetPart = (WorksheetPart) workbookPart.GetPartById(sourceSheet.Id);
    var sourceSheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
    var mergeCells = worksheetPart.Worksheet.Elements<MergeCells>().FirstOrDefault();
    var columns = worksheetPart.Worksheet.Elements<Columns>().FirstOrDefault(); // ��ȡ�п���Ϣ

    // �����¹���������������
    var newSheet = new Sheet
    {
        Name = newSheetName,
        SheetId = (uint) (workbookPart.Workbook.Sheets.Count() + 1),
    };

    newSheet = (Sheet) sourceSheet.CloneNode(deep: true);

    // ��ȡ�������ϲ������¹�����
    var sheets = workbookPart.Workbook.Sheets.Elements<Sheet>().ToList();
    var insertIndex = insertPosition < 0 || insertPosition >= sheets.Count() ? sheets.Count() : insertPosition;
    sheets.Insert(insertIndex, newSheet);

    // ���¹������е� Sheets Ԫ��
    workbookPart.Workbook.Sheets.RemoveAllChildren();
    workbookPart.Workbook.Sheets.Append(sheets);

    // �����¹������ֲ���ʼ�� Worksheet
    var newWorksheetPart = workbookPart.AddNewPart<WorksheetPart>();

    newSheet.Id = workbookPart.GetIdOfPart(newWorksheetPart);

    // ��ʼ��һ���µ� Worksheet �� SheetData
    newWorksheetPart.Worksheet = new Worksheet(new SheetData());

    // ��ȡ�¹������ SheetData
    var newSheetData = newWorksheetPart.Worksheet.GetFirstChild<SheetData>();

    // �����п�����еĻ���
    if (columns != null)
    {
        var newColumns = new Columns();
        foreach (var column in columns.Elements<Column>())
        {
            var newColumn = new Column()
            {
                Min = column.Min,
                Max = column.Max,
                Width = column.Width,
                CustomWidth = column.CustomWidth
            };
            newColumns.Append(newColumn);
        }
        newWorksheetPart.Worksheet.InsertAt(newColumns, 0); // ���п���빤����
    }
    // ���ƺϲ���Ԫ��
    if (mergeCells != null)
    {
        var newMergeCells = new MergeCells();
        foreach (var mergeCell in mergeCells.Elements<MergeCell>())
        {
            newMergeCells.Append(new MergeCell() { Reference = mergeCell.Reference });
        }
        newWorksheetPart.Worksheet.Append(newMergeCells);
    }

    // ��ȿ�¡Դ�������е�ÿһ�в���ӵ��¹�����
    foreach (var row in sourceSheetData.Elements<Row>())
    {
        var clonedRow = (Row) row.CloneNode(true);  // ��¡ÿһ��

        // ���ԭ�����иߣ������и�
        if (row.Height != null)
        {
            clonedRow.Height = row.Height;
            clonedRow.CustomHeight = row.CustomHeight;
        }

        newSheetData.Append(clonedRow);  // ����¡������ӵ��¹�������
    }

    // ��ȡ�򴴽������ַ�����
    var stringTablePart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault()
                           ?? workbookPart.AddNewPart<SharedStringTablePart>();

    // ȷ�������ַ������Ѿ�����
    int stringIndex = AddStringToSharedStringTable("���Բ��������� \n ��������", stringTablePart);

    // �޸ĵ�Ԫ��ȷ��������ȷʹ�ù����ַ�������
    foreach (var row in newSheetData.Elements<Row>())
    {
        foreach (var cell in row.Elements<Cell>())
        {
            if (cell.CellReference == "B22") // ���޸�ָ����Ԫ��
            {
                // ���µ�Ԫ�����������Ϊ SharedString�����Ҹ��¹����ַ�������
                cell.DataType = new EnumValue<DocumentFormat.OpenXml.Spreadsheet.CellValues>(DocumentFormat.OpenXml.Spreadsheet.CellValues.SharedString);
                cell.CellValue = new CellValue(stringIndex.ToString());
            }
        }
    }

    // ������Ĳ������µĹ�������
    newWorksheetPart.Worksheet.Save();
    workbookPart.Workbook.Save(); // ȷ�� Workbook ����

    return newWorksheetPart;
}

int AddStringToSharedStringTable(string test, SharedStringTablePart sharedStringTablePart)
{
    return 0;
}