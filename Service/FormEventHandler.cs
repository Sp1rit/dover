﻿/*
 *  Dover Framework - OpenSource Development framework for SAP Business One
 *  Copyright (C) 2014  Eduardo Piva
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *  
 *  Contact me at <efpiva@gmail.com>
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SAPbouiCOM;
using Dover.Framework.Form;
using System.Reflection;
using Dover.Framework.Factory;
using Castle.Core;
using Castle.Core.Logging;
using System.IO;
using Dover.Framework.Interface;

namespace Dover.Framework.Service
{
    internal class FormEventHandler : MarshalByRefObject, IFormEventHandler
    {
        private SAPbouiCOM.Application sapApp;
        private PermissionManager permissionManager;
        private B1SResourceManager resourceManager;

        public ILogger Logger { get; set; }

        Dictionary<string, EventForm> formEvents = new Dictionary<string, EventForm>();
        Dictionary<string, Type> formImplementationType = new Dictionary<string, Type>();
        Dictionary<string, DoverFormBase> events = new Dictionary<string, DoverFormBase>();
        Dictionary<string, List<DoverFormBase>> pendingForms = new Dictionary<string, List<DoverFormBase>>();

        public FormEventHandler(SAPbouiCOM.Application sapApp, PermissionManager permissionManager,
            B1SResourceManager resourceManager)
        {
            this.sapApp = sapApp;
            this.permissionManager = permissionManager;
            this.resourceManager = resourceManager;
        }


        void IFormEventHandler.RegisterFormLoadBefore(string type, dynamic form)
        {
            this.RegisterFormLoadBefore(type, form);
        }

        private void RegisterFormLoadBefore(string type, DoverFormBase form)
        {
            List<DoverFormBase> pendingList; // forms that does not have UniqueID created by UI.
            if (!pendingForms.TryGetValue(type, out pendingList))
            {
                pendingList = new List<DoverFormBase>();
                pendingForms.Add(type, pendingList);
            }
            pendingList.Add(form);
        }

        void IFormEventHandler.UnRegisterForms()
        {
            foreach (var formEvent in formEvents)
            {
                formEvent.Value.LoadBefore -= new _IEventFormEvents_LoadBeforeEventHandler(this.OnFormLoadBefore);
                formEvent.Value.ActivateAfter -= new _IEventFormEvents_ActivateAfterEventHandler(this.OnFormActivateAfter);
                formEvent.Value.ActivateBefore -= new _IEventFormEvents_ActivateBeforeEventHandler(this.OnFormActivateBefore);
                formEvent.Value.ClickAfter -= new _IEventFormEvents_ClickAfterEventHandler(this.OnFormClickAfter);
                formEvent.Value.ClickBefore -= new _IEventFormEvents_ClickBeforeEventHandler(this.OnFormClickBefore);
                formEvent.Value.CloseAfter -= new _IEventFormEvents_CloseAfterEventHandler(this.OnFormCloseAfter);
                formEvent.Value.CloseBefore -= new _IEventFormEvents_CloseBeforeEventHandler(this.OnFormCloseBefore);
                formEvent.Value.DataAddAfter -= new _IEventFormEvents_DataAddAfterEventHandler(this.OnFormDataAddAfter);
                formEvent.Value.DataAddBefore -= new _IEventFormEvents_DataAddBeforeEventHandler(this.OnFormDataAddBefore);
                formEvent.Value.DataDeleteAfter -= new _IEventFormEvents_DataDeleteAfterEventHandler(this.OnFormDataDeleteAfter);
                formEvent.Value.DataDeleteBefore -= new _IEventFormEvents_DataDeleteBeforeEventHandler(this.OnFormDataDeleteBefore);
                formEvent.Value.DataLoadAfter -= new _IEventFormEvents_DataLoadAfterEventHandler(this.OnFormDataLoadAfter);
                formEvent.Value.DataLoadBefore -= new _IEventFormEvents_DataLoadBeforeEventHandler(this.OnFormDataLoadBefore);
                formEvent.Value.DataUpdateAfter -= new _IEventFormEvents_DataUpdateAfterEventHandler(this.OnFormDataUpdateAfter);
                formEvent.Value.DataUpdateBefore -= new _IEventFormEvents_DataUpdateBeforeEventHandler(this.OnFormDataUpdateBefore);
                formEvent.Value.DeactivateAfter -= new _IEventFormEvents_DeactivateAfterEventHandler(this.OnFormDeactivateAfter);
                formEvent.Value.DeactivateBefore -= new _IEventFormEvents_DeactivateBeforeEventHandler(this.OnFormDeactivateBefore);
                formEvent.Value.KeyDownAfter -= new _IEventFormEvents_KeyDownAfterEventHandler(this.OnFormKeyDownAfter);
                formEvent.Value.KeyDownBefore -= new _IEventFormEvents_KeyDownBeforeEventHandler(this.OnFormKeyDownBefore);
                formEvent.Value.LayoutKeyAfter -= new _IEventFormEvents_LayoutKeyAfterEventHandler(this.OnFormLayoutKeyAfter);
                formEvent.Value.LayoutKeyBefore -= new _IEventFormEvents_LayoutKeyBeforeEventHandler(this.OnFormLayoutKeyBefore);
                formEvent.Value.LoadAfter -= new _IEventFormEvents_LoadAfterEventHandler(this.OnFormLoadAfter);
                formEvent.Value.MenuHighlightAfter -= new _IEventFormEvents_MenuHighlightAfterEventHandler(this.OnFormMenuHighlightAfter);
                formEvent.Value.MenuHighlightBefore -= new _IEventFormEvents_MenuHighlightBeforeEventHandler(this.OnFormMenuHighlightBefore);
                formEvent.Value.PrintAfter -= new _IEventFormEvents_PrintAfterEventHandler(this.OnFormPrintAfter);
                formEvent.Value.PrintBefore -= new _IEventFormEvents_PrintBeforeEventHandler(this.OnFormPrintBefore);
                formEvent.Value.ResizeAfter -= new _IEventFormEvents_ResizeAfterEventHandler(this.OnFormResizeAfter);
                formEvent.Value.ResizeBefore -= new _IEventFormEvents_ResizeBeforeEventHandler(this.OnFormResizeBefore);
                formEvent.Value.RightClickAfter -= new _IEventFormEvents_RightClickAfterEventHandler(this.OnFormRightClickAfter);
                formEvent.Value.RightClickBefore -= new _IEventFormEvents_RightClickBeforeEventHandler(this.OnFormRightClickBefore);
                formEvent.Value.UnloadAfter -= new _IEventFormEvents_UnloadAfterEventHandler(this.OnFormUnloadAfter);
                formEvent.Value.UnloadBefore -= new _IEventFormEvents_UnloadBeforeEventHandler(this.OnFormUnloadBefore);
                formEvent.Value.VisibleAfter -= new _IEventFormEvents_VisibleAfterEventHandler(this.OnFormVisibleAfter);
            }
            // stop any event handling, GC will clean everything.
            formEvents = new Dictionary<string, EventForm>(); 
            formImplementationType = new Dictionary<string, Type>();
            events = new Dictionary<string, DoverFormBase>();
            pendingForms = new Dictionary<string, List<DoverFormBase>>();
        }

        void IFormEventHandler.RegisterForms(bool registerEvents = true)
        {
            if (!registerEvents) // language change.
            {
                resourceManager.Reset();
            }

            Assembly currentAsm = AppDomain.CurrentDomain.Load((string)AppDomain.CurrentDomain.GetData("assemblyName"));
            var formAttributes = (from type in currentAsm.GetTypes()
                                  from attribute in type.GetCustomAttributes(true)
                                  where attribute is SAPbouiCOM.Framework.FormAttribute
                                  select new { 
                                            FormAttribute = (SAPbouiCOM.Framework.FormAttribute)attribute, 
                                            Assembly = currentAsm,
                                            Type = type}).ToList();

            foreach (var asmName in currentAsm.GetReferencedAssemblies())
            {
                try
                {
                    Assembly dependency = AppDomain.CurrentDomain.Load(asmName);
                    formAttributes.AddRange((from type in dependency.GetTypes()
                                             from attribute in type.GetCustomAttributes(true)
                                             where attribute is SAPbouiCOM.Framework.FormAttribute
                                               && typeof(DoverUserFormBase).IsAssignableFrom(type)
                                                 /* do not register systemForm from ReferencedAssemblies, they
                                                  * should be registered by the addin owner.
                                                  * Just ExceptionTrace UserForm can be reused
                                                  */
                                               && (dependency != Assembly.GetExecutingAssembly()
                                                || (dependency == Assembly.GetExecutingAssembly()
                                                      && (((SAPbouiCOM.Framework.FormAttribute)attribute).Resource
                                                               == "Dover.Framework.Form.ExceptionTrace.srf")))
                                             select new
                                             {
                                                 FormAttribute = (SAPbouiCOM.Framework.FormAttribute)attribute,
                                                 Assembly = dependency,
                                                 Type = type
                                             }).ToList());
                }

                catch (TargetInvocationException tie)
                {
                    Logger.Debug(Messages.IgnoringType, tie);
                }
                catch (FileNotFoundException fnfe)
                {
                    Logger.Debug(Messages.IgnoringType, fnfe);
                }
            }

            foreach (var attribute in formAttributes)
            {
                if (registerEvents)
                    RegisterFormEvent(attribute.FormAttribute.FormType, attribute.Type);
                if (!string.IsNullOrEmpty(attribute.FormAttribute.Resource)) // UDOs have their XML stored in the database.
                {
                    resourceManager.ConfigureFormXML(attribute.Assembly, attribute.Type,
                        attribute.FormAttribute.Resource, attribute.FormAttribute.FormType);
                }
            }            
        }

        private void RegisterFormEvent(string formType, Type formImplType)
        {
            if (!formEvents.ContainsKey(formType) && !formImplementationType.ContainsKey(formType))
            {
                if (typeof(DoverSystemFormBase).IsAssignableFrom(formImplType) ||
                        typeof(DoverUDOFormBase).IsAssignableFrom(formImplType)) // we just need this for system and udo forms.
                    formImplementationType.Add(formType, formImplType);

                var eventForm = sapApp.Forms.GetEventForm(formType);
                formEvents.Add(formType, eventForm);
                eventForm.LoadBefore += new _IEventFormEvents_LoadBeforeEventHandler(this.OnFormLoadBefore);
                eventForm.ActivateAfter += new _IEventFormEvents_ActivateAfterEventHandler(this.OnFormActivateAfter);
                eventForm.ActivateBefore += new _IEventFormEvents_ActivateBeforeEventHandler(this.OnFormActivateBefore);
                eventForm.ClickAfter += new _IEventFormEvents_ClickAfterEventHandler(this.OnFormClickAfter);
                eventForm.ClickBefore += new _IEventFormEvents_ClickBeforeEventHandler(this.OnFormClickBefore);
                eventForm.CloseAfter += new _IEventFormEvents_CloseAfterEventHandler(this.OnFormCloseAfter);
                eventForm.CloseBefore += new _IEventFormEvents_CloseBeforeEventHandler(this.OnFormCloseBefore);
                eventForm.DataAddAfter += new _IEventFormEvents_DataAddAfterEventHandler(this.OnFormDataAddAfter);
                eventForm.DataAddBefore += new _IEventFormEvents_DataAddBeforeEventHandler(this.OnFormDataAddBefore);
                eventForm.DataDeleteAfter += new _IEventFormEvents_DataDeleteAfterEventHandler(this.OnFormDataDeleteAfter);
                eventForm.DataDeleteBefore += new _IEventFormEvents_DataDeleteBeforeEventHandler(this.OnFormDataDeleteBefore);
                eventForm.DataLoadAfter += new _IEventFormEvents_DataLoadAfterEventHandler(this.OnFormDataLoadAfter);
                eventForm.DataLoadBefore += new _IEventFormEvents_DataLoadBeforeEventHandler(this.OnFormDataLoadBefore);
                eventForm.DataUpdateAfter += new _IEventFormEvents_DataUpdateAfterEventHandler(this.OnFormDataUpdateAfter);
                eventForm.DataUpdateBefore += new _IEventFormEvents_DataUpdateBeforeEventHandler(this.OnFormDataUpdateBefore);
                eventForm.DeactivateAfter += new _IEventFormEvents_DeactivateAfterEventHandler(this.OnFormDeactivateAfter);
                eventForm.DeactivateBefore += new _IEventFormEvents_DeactivateBeforeEventHandler(this.OnFormDeactivateBefore);
                eventForm.KeyDownAfter += new _IEventFormEvents_KeyDownAfterEventHandler(this.OnFormKeyDownAfter);
                eventForm.KeyDownBefore += new _IEventFormEvents_KeyDownBeforeEventHandler(this.OnFormKeyDownBefore);
                eventForm.LayoutKeyAfter += new _IEventFormEvents_LayoutKeyAfterEventHandler(this.OnFormLayoutKeyAfter);
                eventForm.LayoutKeyBefore += new _IEventFormEvents_LayoutKeyBeforeEventHandler(this.OnFormLayoutKeyBefore);
                eventForm.LoadAfter += new _IEventFormEvents_LoadAfterEventHandler(this.OnFormLoadAfter);
                eventForm.MenuHighlightAfter += new _IEventFormEvents_MenuHighlightAfterEventHandler(this.OnFormMenuHighlightAfter);
                eventForm.MenuHighlightBefore += new _IEventFormEvents_MenuHighlightBeforeEventHandler(this.OnFormMenuHighlightBefore);
                eventForm.PrintAfter += new _IEventFormEvents_PrintAfterEventHandler(this.OnFormPrintAfter);
                eventForm.PrintBefore += new _IEventFormEvents_PrintBeforeEventHandler(this.OnFormPrintBefore);
                eventForm.ResizeAfter += new _IEventFormEvents_ResizeAfterEventHandler(this.OnFormResizeAfter);
                eventForm.ResizeBefore += new _IEventFormEvents_ResizeBeforeEventHandler(this.OnFormResizeBefore);
                eventForm.RightClickAfter += new _IEventFormEvents_RightClickAfterEventHandler(this.OnFormRightClickAfter);
                eventForm.RightClickBefore += new _IEventFormEvents_RightClickBeforeEventHandler(this.OnFormRightClickBefore);
                eventForm.UnloadAfter += new _IEventFormEvents_UnloadAfterEventHandler(this.OnFormUnloadAfter);
                eventForm.UnloadBefore += new _IEventFormEvents_UnloadBeforeEventHandler(this.OnFormUnloadBefore);
                eventForm.VisibleAfter += new _IEventFormEvents_VisibleAfterEventHandler(this.OnFormVisibleAfter);
            }
            else
            {
                throw new ArgumentException(string.Format(Messages.FormTypeNotUnique, formType));
            }
        }

        void IFormEventHandler.RegisterForm(string uniqueID, dynamic form)
        {
            this.RegisterForm(uniqueID, form);
        }

        /*
         * Register form so we can get FormUID when form is being created.
         */
        private void RegisterForm(string uniqueID, DoverFormBase form)
        {
            if (!events.ContainsKey(uniqueID)) // prevent duplicates. Shouldn't happen.
            {
                events.Add(uniqueID, form);
                form.OnInitializeFormEvents();
            }
        }

        protected virtual void OnFormLoadBefore(SBOItemEventArg pVal, out bool BubbleEvent)
        {
            List<DoverFormBase> pendingList; // forms that does not have UniqueID.
            Type implementationType;
            IForm form = sapApp.Forms.Item(pVal.FormUID);
            string key = form.TypeEx;
            BubbleEvent = true;

            if (pendingForms.TryGetValue(key, out pendingList))
            {
                pendingForms.Remove(key);
                foreach (var addOneForm in pendingList)
                {
                    addOneForm.OnFormLoadBefore(pVal, out BubbleEvent);
                }
            }
            else if (formImplementationType.TryGetValue(key, out implementationType) && !events.ContainsKey(pVal.FormUID))
            // if no form was registered, it's a system or udo form.
            {
                DoverFormBase addOneSysForm = (DoverFormBase)ContainerManager.Container.Resolve(implementationType);
                addOneSysForm.FormUID = pVal.FormUID;
                RegisterForm(pVal.FormUID, addOneSysForm);
                addOneSysForm.OnFormLoadBefore(pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormLoadAfter(SBOItemEventArg pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormLoadAfter(pVal);
            }
        }

        protected virtual void OnFormUnloadBefore(SBOItemEventArg pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormUnloadBefore(pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormUnloadAfter(SBOItemEventArg pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormUnloadAfter(pVal);
            }
        }

        protected virtual void OnFormActivateBefore(SBOItemEventArg pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormActivateBefore(pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormActivateAfter(SBOItemEventArg pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormActivateAfter(pVal);
            }
        }

        protected virtual void OnFormDeactivateBefore(SBOItemEventArg pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormDeactivateBefore(pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormDeactivateAfter(SBOItemEventArg pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormDeactivateAfter(pVal);
            }
        }

        protected virtual void OnFormResizeBefore(SBOItemEventArg pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormResizeBefore(pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormResizeAfter(SBOItemEventArg pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormResizeAfter(pVal);
            }
        }

        protected virtual void OnFormMenuHighlightBefore(SBOItemEventArg pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormMenuHighlightBefore(pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormMenuHighlightAfter(SBOItemEventArg pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormMenuHighlightAfter(pVal);
            }
        }

        protected virtual void OnFormDataAddBefore(ref BusinessObjectInfo pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormDataAddBefore(ref pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormDataAddAfter(ref BusinessObjectInfo pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormDataAddAfter(ref pVal);
            }
        }

        protected virtual void OnFormDataUpdateBefore(ref BusinessObjectInfo pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormDataUpdateBefore(ref pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormDataUpdateAfter(ref BusinessObjectInfo pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormDataUpdateAfter(ref pVal);
            }
        }

        protected virtual void OnFormDataDeleteBefore(ref BusinessObjectInfo pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormDataDeleteBefore(ref pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormDataDeleteAfter(ref BusinessObjectInfo pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormDataDeleteAfter(ref pVal);
            }
        }

        protected virtual void OnFormDataLoadBefore(ref BusinessObjectInfo pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormDataLoadBefore(ref pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormDataLoadAfter(ref BusinessObjectInfo pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormDataLoadAfter(ref pVal);
            }
        }

        protected virtual void OnFormKeyDownBefore(SBOItemEventArg pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormKeyDownBefore(pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormKeyDownAfter(SBOItemEventArg pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormKeyDownAfter(pVal);
            }
        }

        protected virtual void OnFormLayoutKeyBefore(ref LayoutKeyInfo pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormLayoutKeyBefore(ref pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormLayoutKeyAfter(ref LayoutKeyInfo pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormLayoutKeyAfter(ref pVal);
            }
        }

        protected virtual void OnFormPrintBefore(ref PrintEventInfo pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormPrintBefore(ref pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormPrintAfter(ref PrintEventInfo pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormPrintAfter(ref pVal);
            }
        }

        protected virtual void OnFormClickBefore(SBOItemEventArg pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormClickBefore(pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormClickAfter(SBOItemEventArg pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormClickAfter(pVal);
            }
        }

        protected virtual void OnFormRightClickBefore(ref ContextMenuInfo pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormRightClickBefore(ref pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormRightClickAfter(ref ContextMenuInfo pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormRightClickAfter(ref pVal);
            }
        }

        protected virtual void OnFormCloseBefore(SBOItemEventArg pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormCloseBefore(pVal, out BubbleEvent);
            }
        }

        protected virtual void OnFormCloseAfter(SBOItemEventArg pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormCloseAfter(pVal);
                events.Remove(pVal.FormUID);
            }
        }

        protected virtual void OnFormVisibleAfter(SBOItemEventArg pVal)
        {
            DoverFormBase addOneForm;
            if (events.TryGetValue(pVal.FormUID, out addOneForm))
            {
                addOneForm.OnFormVisibleAfter(pVal);
            }
        }
    }

}
