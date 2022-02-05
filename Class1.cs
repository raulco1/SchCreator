using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace SchCreator
{
    [Transaction(TransactionMode.Manual)]
    public class Class1:IExternalCommand
    {
        //built in parameters fields to add in schedule
        BuiltInParameter[] BiParams = new BuiltInParameter[] { BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
            BuiltInParameter.WALL_BASE_CONSTRAINT, BuiltInParameter.HOST_AREA_COMPUTED, BuiltInParameter.CURVE_ELEM_LENGTH};
        public Result Execute(ExternalCommandData edata, ref string message, ElementSet elements)
        {
            //UI Document 
            UIDocument uidoc = edata.Application.ActiveUIDocument;
            //the current document
            Document doc = uidoc.Document;
            //transaction
            Transaction t = new Transaction(doc, "Create Schedule");
            t.Start();

            //Get Category ID
            //Variable need to be edited
            ElementId pipeAssenblyCategoryId = new ElementId(BuiltInCategory.OST_Walls);
            ViewSchedule schedule = ViewSchedule.CreateSchedule(doc, pipeAssenblyCategoryId);
            schedule.Name = "Pipe Accessory test";

            //family and type group sorting
            ScheduleSortGroupField FamilyTypeSorting = null;
            //base constraint filter
            ScheduleFilter BaseConstraintFilter = null;

            //iterate schedule fields 
            foreach (SchedulableField sf in schedule.Definition.GetSchedulableFields())
            {
                //Check field if in params
                if (CheckField(sf))
                {
                    //add field 
                    ScheduleField scheduleField = schedule.Definition.AddField(sf);
                    //schedule's filter (to collect base constraint)
                    if (sf.ParameterId == new ElementId(BuiltInParameter.WALL_BASE_CONSTRAINT))
                    {
                        BaseConstraintFilter = new ScheduleFilter(scheduleField.FieldId, ScheduleFilterType.Equal,
                            GetLevelByName(doc, "Level 1").Id);
                        //add schedule's filter
                        schedule.Definition.AddFilter(BaseConstraintFilter);
                    }

                    //schedule's group sorting (to collect family and type field)
                    if (sf.ParameterId == new ElementId(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM))
                    {
                        //Create group sorting (family and type)
                        FamilyTypeSorting = new ScheduleSortGroupField(scheduleField.FieldId);
                        //add schedule's group sorting field
                        schedule.Definition.AddSortGroupField(FamilyTypeSorting);
                    }
                }
            }
            //set filter 
            schedule.Definition.SetFilter(0, BaseConstraintFilter);
            //set sorting/grouping
            schedule.Definition.SetSortGroupField(0, FamilyTypeSorting);
            //close transaction
            t.Commit();
            //set active view
            uidoc.ActiveView = schedule;

            //return result
            return Result.Succeeded;
        }

        public bool CheckField(SchedulableField vs)
        {
            foreach (BuiltInParameter bip in BiParams)
            {
                if (new ElementId(bip) == vs.ParameterId)
                    return true;
            }
            return false;
        }

        public Element GetLevelByName(Document doc, string name)
        {
            Level level = new FilteredElementCollector(doc).OfClass(typeof(Level))
                .Cast<Level>().Where(x => x.Name == name).FirstOrDefault();
            return level;
        }
    }
}
