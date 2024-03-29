// --- Copyright (c) notice NevoWeb ---
//  Copyright (c) 2015 SARL Nevoweb.  www.Nevoweb.com. The MIT License (MIT).
// Author: D.C.Lee
// ------------------------------------------------------------------------
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ------------------------------------------------------------------------
// This copyright notice may NOT be removed, obscured or modified without written consent from the author.
// --- End copyright notice --- 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Exceptions;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security;
using DotNetNuke.Services.Localization;
using NBrightMod.common;
using System.Web.UI;

namespace Nevoweb.DNN.NBrightMod
{

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The ViewNBrightGen class displays the content
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class View : Base.NBrightModBase, IActionable
    {

        private bool _versionRecordExist = false;

        #region Event Handlers


        protected override void OnInit(EventArgs e)
        {

            base.OnInit(e);

            // check for version records 
            if (Request.IsAuthenticated && (LocalUtils.VersionUserCanValidate(ModuleId) || LocalUtils.VersionUserMustCreateVersion(ModuleId)))
            {
                    _versionRecordExist = LocalUtils.HasVersion(ModuleId);
            }

            LocalUtils.IncludePageHeaders(base.ModuleId.ToString(""), this.Page, "NBrightMod","view");
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {

                base.OnLoad(e);

                if (Page.IsPostBack == false)
                {
                    // check we have settings
                    var settings = LocalUtils.GetSettings(ModuleId.ToString());

                    if (settings.ModuleId == 0 || settings.GetXmlProperty("genxml/dropdownlist/themefolder") == "")
                    {
                        var lit = new Literal();
                        var rtnValue = Localization.GetString("notheme", "/DesktopModules/NBright/NBrightMod/App_LocalResources/View.ascx.resx", PortalSettings.Current, Utils.GetCurrentCulture(), true);
                        lit.Text = rtnValue;
                        phData.Controls.Add(lit);
                    }
                    else
                    {
                        PageLoad();
                    }
                }
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        private void PageLoad()
        {
            var objCtrl = new NBrightDataController();

            var settings = LocalUtils.GetSettings(ModuleId.ToString());
            var activatedetail = settings.GetXmlPropertyBool("genxml/checkbox/activatedetail");
            var debug = settings.GetXmlPropertyBool("genxml/checkbox/debugmode");
            // check for detail page display
            var displayview = "view.cshtml";

            var eid = Utils.RequestQueryStringParam(Request, "eid");
            // check for detail page display
            if (Utils.IsNumeric(eid) && activatedetail)
            {
                var info = objCtrl.Get(Convert.ToInt32(eid), Utils.GetCurrentCulture());

                var pagename = info.GetXmlProperty("genxml/lang/genxml/textbox/pagename");
                if (pagename == "") pagename = info.GetXmlProperty("genxml/textbox/pagename");
                if (pagename == "") pagename = info.GetXmlProperty("genxml/lang/genxml/textbox/title");
                if (pagename == "") pagename = info.GetXmlProperty("genxml/textbox/title");

                var pagetitle = info.GetXmlProperty("genxml/lang/genxml/textbox/pagetitle");
                if (pagetitle == "") pagetitle = info.GetXmlProperty("genxml/textbox/pagetitle");
                if (pagetitle == "") pagetitle = info.GetXmlProperty("genxml/lang/genxml/textbox/title");
                if (pagetitle == "") pagetitle = info.GetXmlProperty("genxml/textbox/title");

                var pagekeywords = info.GetXmlProperty("genxml/lang/genxml/textbox/pagekeywords");

                var pagedescription = info.GetXmlProperty("genxml/lang/genxml/textbox/pagedescription");

                DotNetNuke.Framework.CDefault tp = (DotNetNuke.Framework.CDefault)this.Page;
                if (pagetitle != "") tp.Title = pagetitle;
                if (pagedescription != "") tp.Description = pagedescription;
                if (pagekeywords != "") tp.KeyWords = pagekeywords;

                if (info.GetXmlProperty("genxml/imgs/genxml[1]/hidden/imageurl") != "")
                {
                    tp.Header.Controls.Add(new LiteralControl(String.Format("<meta id=\"ogimage\" property=\"og:image\" content=\"{0}\" />", "https://" + DnnUtils.GetCurrentPortalSettings().DefaultPortalAlias.TrimEnd('/') + "/" + info.GetXmlProperty("genxml/imgs/genxml[1]/hidden/imageurl").TrimStart('/'))));
                }

                displayview = "viewdetail.cshtml";
            }
            else
            {
                eid = ""; // clear for cache key
            }

            var strOut = "";
            var cacheKey = "nbrightmodview-" + PortalSettings.Current.PortalId + "-" + ModuleId + "-" + Utils.GetCurrentCulture() + "-" + eid;
            var razorCacheKey = settings.GetXmlProperty("genxml/dropdownlist/themefolder") + Utils.GetCurrentCulture();
            if (LocalUtils.VersionUserCanValidate(ModuleId) || LocalUtils.VersionUserMustCreateVersion(ModuleId))
            {
                // add userid for users with version rights.
                cacheKey += "-" + UserId;
                razorCacheKey += "-" + UserId;
            }

            // Set cache using ecomode cookie.
            var ecomode = "off";
            var ecomodeCookie = HttpContext.Current.Request.Cookies["ecomode"];
            if (ecomodeCookie != null) ecomode = ecomodeCookie.Value;
            cacheKey += ecomode;

            if (!debug) strOut = (String)LocalUtils.GetRazorCache(cacheKey, ModuleId.ToString());

            var hasEditAccess = false;
            if (UserId > 0) hasEditAccess = DotNetNuke.Security.Permissions.ModulePermissionController.CanEditModuleContent(this.ModuleConfiguration);
            if (hasEditAccess) strOut = ""; // don;t use cache in edit mode.

            if (String.IsNullOrWhiteSpace(strOut)) // check if we already have razor cache
            {
                // preprocess razor template to get meta data for data select into cache.
                var cachedlist = LocalUtils.RazorPreProcessTempl(displayview, ModuleId.ToString(""), Utils.GetCurrentCulture());
                var orderby = "";
                if (cachedlist != null && cachedlist.ContainsKey("orderby")) orderby = cachedlist["orderby"];
                var filter = "";
                if (cachedlist != null && cachedlist.ContainsKey("filter")) filter = cachedlist["filter"];
                if (Utils.IsNumeric(eid) && activatedetail)
                {
                    filter = " and NB1.ItemId = '" + eid + "'";
                }

                // get source moduleid
                var sourcemodid = Convert.ToInt32(ModuleId);
                if (settings.GUIDKey != settings.GetXmlProperty("genxml/dropdownlist/datasourceref") && settings.GetXmlProperty("genxml/dropdownlist/datasourceref") != "")
                {
                    var sourcesettings = objCtrl.GetByGuidKey(PortalSettings.Current.PortalId, -1, "SETTINGS", settings.GetXmlProperty("genxml/dropdownlist/datasourceref"));
                    if (sourcesettings == null)
                    {
                        // source module may have been removed. so reset it.
                        settings.SetXmlProperty("genxml/dropdownlist/datasourceref", "");
                        LocalUtils.UpdateSettings(settings);
                    }
                    else
                    {
                        sourcemodid = sourcesettings.ModuleId;
                    }
                }

                // get data list
                var returnLimit = settings.GetXmlPropertyInt("genxml/textbox/returnlimit");
                var pageSize = settings.GetXmlPropertyInt("genxml/textbox/pagesize");
                var pgnum = Utils.RequestQueryStringParam(Request, "pgnum");
                var pageNumber = 0;
                if (Utils.IsNumeric(pgnum)) pageNumber = Convert.ToInt32(pgnum);

                var l = objCtrl.GetList(PortalSettings.Current.PortalId, sourcemodid, "NBrightModDATA", filter, orderby, returnLimit, pageNumber, pageSize, 0, Utils.GetCurrentCulture());

                //if (Request.IsAuthenticated && Session["nbrightmodversion"] != null && Session["nbrightmodversion"].ToString() == "1")
                if (Request.IsAuthenticated)
                {
                        // get any version data.
                        var length = l.Count;
                    var removeList = new List<int>();
                    for (int i = 0; i < length; i++)
                    {
                        var nbi = l[i];
                        if (nbi.XrefItemId > 0)
                        {
                            if (nbi.GetXmlPropertyBool("genxml/versiondelete"))
                            {
                                removeList.Add(nbi.ItemID);
                            }
                            else
                            {
                                var vnbi = objCtrl.GetData(nbi.XrefItemId, "vNBrightModDATALANG", nbi.Lang, true);
                                l[i] = vnbi;
                            }
                        }
                    }
                    // remove deleted record.
                    for (int i = length - 1; i >= 0; i--)
                    {
                        if (removeList.Contains(l[i].ItemID))
                        {
                            l.RemoveAt(i);
                        }
                    }

                    // get any "added" version records
                    var l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "aNBrightModDATA", "", orderby, 0, 0, 0, 0, Utils.GetCurrentCulture());
                    foreach (var nbi in l2)
                    {
                        l.Add(nbi);
                    }

                    if (l.Count > 0 && !String.IsNullOrWhiteSpace(orderby))
                    {
                        // need to put the sort correct, but must be done at SQL level, because we have dynamic sort defined.
                        var filter2 = " and ( ";
                        foreach (var nbi in l)
                        {
                            filter2 += " NB1.ItemId = " + nbi.ItemID + " or ";
                        }
                        filter2 = filter2.Substring(0, filter2.Length - 3) + ") ";
                        l = objCtrl.GetList(PortalSettings.Current.PortalId, sourcemodid, "", filter2, orderby, returnLimit, pageNumber, pageSize, 0, Utils.GetCurrentCulture());
                    }

                }

                    var divclass = "";
                if (settings.GetXmlProperty("genxml/textbox/wrapperclass") != "")
                {
                    divclass = "class='" + settings.GetXmlProperty("genxml/textbox/wrapperclass") + "'";
                }
                strOut = "<div id='" + settings.GetXmlProperty("genxml/hidden/modref") + "'  " + divclass + ">";
                strOut += LocalUtils.RazorTemplRenderList(displayview, ModuleId.ToString(""), "" , l, Utils.GetCurrentCulture(), debug);
                strOut += "</div>";

                if (!debug)
                {
                    // save razor compiled output, for performace
                    LocalUtils.SetRazorCache(cacheKey, strOut,ModuleId.ToString(""));
                }
            }

            if (_versionRecordExist)
            {
                //inject JS to mark container.
                strOut = "<div class='actionMenu'><ul class='dnn_mact' style='left:0;top:0;position:absolute;'><li><a href='javascript:void(0)' style='background-color:#cd1719;'><i class='fa fa-exclamation-circle'></i></a></li></ul></div>" + strOut;
            }

            var lit = new Literal();
            lit.Text = strOut;
            phData.Controls.Add(lit);

        }

        #endregion


        #region Optional Interfaces

        public ModuleActionCollection ModuleActions
        {
            get
            {
                var settings = LocalUtils.GetSettings(ModuleId.ToString());
                var actions = new ModuleActionCollection();
                if (settings.GUIDKey == settings.GetXmlProperty("genxml/dropdownlist/datasourceref") || settings.GetXmlProperty("genxml/dropdownlist/datasourceref") == "")
                {
                    actions.Add(GetNextActionID(), Localization.GetString("EditModule", this.LocalResourceFile), "", "", "", EditUrl(), false, SecurityAccessLevel.Edit, true, false);
                }

                if (_versionRecordExist)
                {
                    actions.Add(GetNextActionID(), Localization.GetString("Version1", this.LocalResourceFile), "", "", "translate.gif", EditUrl() + "?version=1", false, SecurityAccessLevel.Edit, true, false);
                    actions.Add(GetNextActionID(), Localization.GetString("Version0", this.LocalResourceFile), "", "", "translated.gif", EditUrl() + "?version=0", false, SecurityAccessLevel.Edit, true, false);
                    if (LocalUtils.VersionUserCanValidate(ModuleId))
                    {
                        actions.Add(GetNextActionID(), Localization.GetString("Version2", this.LocalResourceFile), "", "", "grant.gif", EditUrl() + "?version=2", false, SecurityAccessLevel.Edit, true, false);
                        actions.Add(GetNextActionID(), Localization.GetString("Version4", this.LocalResourceFile), "", "", "error-icn.png", EditUrl() + "?version=4", false, SecurityAccessLevel.Edit, true, false);
                        actions.Add(GetNextActionID(), Localization.GetString("Version3", this.LocalResourceFile), "", "", "delete.gif", EditUrl() + "?version=3", false, SecurityAccessLevel.Edit, true, false);
                    }
                    else
                    {
                        actions.Add(GetNextActionID(), Localization.GetString("Version5", this.LocalResourceFile), "", "", "icon-validate-success.png", EditUrl() + "?version=5", false, SecurityAccessLevel.Edit, true, false);
                        if (settings.GetXmlPropertyBool("genxml/versionemailsent"))
                        {
                            actions.Add(GetNextActionID(), Localization.GetString("Version3", this.LocalResourceFile), "", "", "delete.gif", EditUrl() + "?version=3", false, SecurityAccessLevel.Edit, true, false);
                        }
                        else
                        {
                            actions.Add(GetNextActionID(), Localization.GetString("Version6", this.LocalResourceFile), "", "", "delete.gif", EditUrl() + "?version=6", false, SecurityAccessLevel.Edit, true, false);
                        }

                    }
                }
                actions.Add(GetNextActionID(), Localization.GetString("auditreport", this.LocalResourceFile), "", "", "about.gif", EditUrl() + "?auditlog=1", false, SecurityAccessLevel.Edit, true, false);

                actions.Add(GetNextActionID(), Localization.GetString("Refresh", this.LocalResourceFile), "", "", "action_refresh.gif", EditUrl() + "?refreshview=1", false, SecurityAccessLevel.Edit, true, false);
                actions.Add(GetNextActionID(), Localization.GetString("Tools", this.LocalResourceFile), "", "", "action_source.gif", EditUrl("Tools") , false, SecurityAccessLevel.Admin, true, false);
                actions.Add(GetNextActionID(), Localization.GetString("ThemeManager", this.LocalResourceFile), "", "", "manage-icn.png", EditUrl("ThemeManager"), false, SecurityAccessLevel.Host, true, false);
                return actions;
            }
        }

        #endregion



    }

}
