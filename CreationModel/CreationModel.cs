using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CreationModel
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIDocument = commandData.Application.ActiveUIDocument;
            Document document = uIDocument.Document;

            Level level1 = GetLevelByName(document, "Уровень 1");
            Level level2 = GetLevelByName(document, "Уровень 2");

            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(document);
            transaction.Start("Создание стен");
            for (int i = 0; i < 4; i++)
            {
                Wall wall = CreateWallByPoints(document, points[i], points[i + 1], level1.Id, level2.Id);
                walls.Add(wall);
                if (i == 0)
                    AddDoorOrWindow(document, level1, walls[0], BuiltInCategory.OST_Doors, "0915 x 2134 мм", "Одиночные-Щитовые");
                else
                    AddDoorOrWindow(document, level1, walls[i], BuiltInCategory.OST_Windows, "0915 x 1830 мм", "Фиксированные");

            }

            transaction.Commit();

            return Result.Succeeded;
        }

        public void AddDoorOrWindow(Document document, Level level1, Wall wall, BuiltInCategory builtInCategory, string typeName, string familyName)
        {
            FamilySymbol familySymbol = new FilteredElementCollector(document)
                      .OfClass(typeof(FamilySymbol))
                      .OfCategory(builtInCategory)
                      .OfType<FamilySymbol>()
                      .Where(x => x.Name.Equals(typeName))
                      .Where(x => x.FamilyName.Equals(familyName))
                      .FirstOrDefault();

            if (!familySymbol.IsActive)
                familySymbol.Activate();
    
            LocationCurve curve = wall.Location as LocationCurve;
            XYZ point1 = curve.Curve.GetEndPoint(0);
            XYZ point2 = curve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            document.Create.NewFamilyInstance(point, familySymbol, wall, level1, StructuralType.NonStructural);
        }

        public Level GetLevelByName(Document document, string name)
        {
            Level level = new FilteredElementCollector(document)
                     .OfClass(typeof(Level))
                     .OfType<Level>()
                     .Where(x => x.Name.Equals(name))
                     .FirstOrDefault();
            return level;

        }
        public Level GetFamilySymbolByName(Document document, string name)
        {
            Level level = new FilteredElementCollector(document)
                     .OfClass(typeof(Level))
                     .OfType<Level>()
                     .Where(x => x.Name.Equals(name))
                     .FirstOrDefault();
            return level;

        }
        public Wall CreateWallByPoints(Document document, XYZ point1, XYZ point2, ElementId level1Id, ElementId level2Id)
        {
            Line line = Line.CreateBound(point1, point2);
            Wall wall = Wall.Create(document, line, level1Id, false);
            wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2Id);
            return wall;
        }
    }



}
