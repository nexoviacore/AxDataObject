
var iframeHtmlSrc = "";
function createPopup(iframeSource, width, height) {
    iframeHtmlSrc = iframeSource
    width = width || "100vw";

    htmlContent = createIframeMarkup(iframeSource, width, height);
    $("head").append(htmlContent);

    var options = { "closeOnOutsideClick": true, "hashTracking": false, "closeOnEscape": false };

    var inst = $('[data-remodal-id=axpertPopupModal]:not(.remodal-is-initialized):not(.remodal-is-closed):eq(0)').remodal(options);
    if (inst && inst.state != "opened")
        inst.open();

    return inst;
}

function createIframeMarkup(iframeSource, width, height) {
    var sizeCss = "";
    if (width != undefined) {
        sizeCss = "width:" + width + ";";
    }
    if (height != undefined) {
        sizeCss += "height:" + height + ";";
    }
    var $markup = '<div id="axpertPopupWrapper" style="' + sizeCss + '" class="remodal" data-remodal-id="axpertPopupModal">';
    $markup += '<button data-remodal-action="close" class="remodal-close remodalCloseBtn icon-basic-remove" title="Close"></button>';
    $markup += "<div style='height:100%;' id='iframeMarkUp'></div>"
    $markup += '</div>';
    return $markup;
}
//$(document).on('opening', '#axpertPopupWrapper', function () {
//    $("#wrapperForMainNewData", eval(callParent('wrapperForMainNewData', 'id'))).hide();
//});

//function checkForAxpPopUpExists() {
//    if (eval(callParent('axpertPopupWrapper', 'id'))) {
//        //
//        return $("#axpertPopupWrapper", eval(callParent('axpertPopupWrapper', 'id'))).length;
//    }
//    else
//        return false;
//}

//$(document).on('closing', '#axpertPopupWrapper', function () {
//    if (!checkForAxpPopUpExists())
//        $("#wrapperForMainNewData", eval(callParent('wrapperForMainNewData', 'id'))).show();

//    if(isBackBtnHidden){
//        $(callParentNew("appBackBtn","class")).show();
//        isBackBtnHidden = false;
//    }
//});

$(document).on('opened', '#axpertPopupWrapper', function () {

    $("#axpertPopupWrapper #iframeMarkUp").html('<iframe src="javascript:void(0);" id="popupIframeRemodal" width="100%" height="100%" style="border:0px; "></iframe>');
    //loadHTMLInPopup(iframeHtmlSrc);
    //if (window.leftMenuWrapper === undefined) {
    //    //else it is the main frame should not go beyond this to avoid Cross Frame Origin

    //    if (window.parent.document) {
    //        $(window.parent.document).contents().find(".remodalCloseBtn").hide();
    //    }

    //}


    //$("#popupIframeRemodal").on("load", function () {
    //    if ($("#popupIframeRemodal")[0].contentWindow.$) {
    //        $("#popupIframeRemodal").contents().find("head")
    //      .append($("<style>html{overflow:hidden;}#backforwrdbuttons{display:none;}a[title=\"List View\"]{display: none !important;}#new{display: none !important;}#dvGoBack{display: none !important;}#ivInSearch{right:42px;}.requestJSON.isMobile #ivInSearch{right:0px;}.btextDir-rtl #ivInSearch{left:25px;right:auto;}.requestJSON.isMobile #ivInSearch ul#iconsUl,.requestJSON.isMobile #ivInSearch ul#iconsExportUl,.requestJSON.isMobile #ivInSearch ul.dropDownButton__list.dropdown-menu{right:-7px !important;}.btextDir-rtl.requestJSON.isMobile #ivInSearch ul#iconsUl,.btextDir-rtl.requestJSON.isMobile #ivInSearch ul#iconsExportUl,.btextDir-rtl.requestJSON.isMobile #ivInSearch ul.dropDownButton__list.dropdown-menu{left:-12px !important;right:auto !important;}.requestJSON.isMobile div#searchBar #iconsNew .searchBoxChildContainer{right: -17px !important;}.btextDir-rtl.requestJSON.isMobile div#searchBar #iconsNew .searchBoxChildContainer{left:0px !important;right: auto!important;}.btextDir-rtl div#searchBar{right:-16px !important;}</style>"));
    //        $("#popupIframeRemodal").contents().find("head")
    //     .append($("<script>$(document).ready(function() { if ($('[id^=gridToggleBtn]').length > 0 && recordid !='0') { $($('[id^=gridToggleBtn]')).each(function (index) { toggleTheEditLayout($('[id^=gridToggleBtn]')[index].id.substr($('[id^=gridToggleBtn]')[index].id.indexOf('gridToggleBtn') + 13)); }); } });</script>"));
    //        // hiding popup struct buttons except save
    //    }
    //    try {
    //        if (eval(callParent('isTstructPopup'))) {
    //            $("#popupIframeRemodal").contents().find("head")
    //      .append($("<style>#icons li a:not([title=Save]){display: none !important;}</style>"));
    //        }
    //    }
    //    catch (ex) {
    //        console.log(ex.message);
    //    }

    //    try{
    //        if($(callParentNew("appBackBtn","class")).is(":visible") && (findGetParameter("axispop", iframeHtmlSrc) || findGetParameter("axpop", iframeHtmlSrc) || eval(callParent('isTstructPopup')))){
    //            $(callParentNew("appBackBtn","class")).hide();
    //            isBackBtnHidden = true;
    //        }
    //    }catch(ex){}

    //    try {
    //        ax_loadCustomPopPage(iframeHtmlSrc, window);
    //    } catch (ex) { }

    //    //end
    //    ShowDimmer(false);
    //});

    //$("#dvSelectedGlobalVar,#ExportImportCogIcon", eval(callParent('ExportImportCogIcon', 'id'))).hide();
    //$("#popupIframeRemodal").contents().find('body :focusable').first().focus();
    
    //MainNewEdit = true;
});

//$(document).on('closed', '#axpertPopupWrapper', function () {
//    if (window.leftMenuWrapper === undefined) {
//        //else it is the main frame should not go beyond this to avoid Cross Frame Origin

//        if (window.parent.document) {
//            $(window.parent.document).contents().find(".remodalCloseBtn").show();
//        }

//    }
//    var isAxPop = $("#axpertPopupWrapper").find("#popupIframeRemodal").attr("src").indexOf("AxPop=true") > -1;
//    var inst = $('[data-remodal-id=axpertPopupModal]:eq(0)').remodal();
//    try {
//        inst.destroy();
//    } catch (ex) { }
//    if (!checkForAxpPopUpExists())
//        $("#ExportImportCogIcon", eval(callParent('ExportImportCogIcon', 'id'))).show();

//    if (!checkForAxpPopUpExists())
//        $("#dvSelectedGlobalVar", eval(callParent('dvSelectedGlobalVar', 'id'))).show();
//    if (!checkForAxpPopUpExists())
//        $("#wrapperForMainNewData", eval(callParent('wrapperForMainNewData', 'id'))).show();
//    MainNewEdit = false;
//    if (isAxPop && (window.document.title == "Iview" || window.document.title == "List IView") && eval(callParent('isSuccessAlertInPopUp'))) {
//        eval(callParent('isSuccessAlertInPopUp') + "= false");
//        pushValToSession('IsFromChildWindow', 'true');
//        if (eval(callParent('isRefreshParentOnClose'))) {
//            eval(callParent('isRefreshParentOnClose') + "= false");
//            window.location.href = window.location.href;
//        }
//    } else if (isAxPop && (window.document.title == "Load TStruct with QS" || window.document.title == "Tstruct" || window.document.title == "Load Tstruct") && eval(callParent('isSuccessAlertInPopUp'))) {
//        eval(callParent('isSuccessAlertInPopUp') + "= false");
//        let ReloadParent = true;
//        try {
//            var CallActionVar = eval(callParent('callBackFunDtls'));
//            if (typeof CallActionVar != "undefined" && CallActionVar.indexOf("♠") > -1) {//On Action call 
//                let CallActionName = CallActionVar.split("♠").length == 5 ? CallActionVar.split("♠")[1] : "";
//                if (CallActionName != "") {
//                    var actIndex = $j.inArray(CallActionName, tstActionName);
//                    if (actIndex == -1)
//                        actIndex = $j.inArray(CallActionName.toLowerCase(), tstActionName);
//                    ReloadParent = actParRefresh[actIndex].toLowerCase() == "true" ? true : false;
//                }
//            }
//        }
//        catch (ex) { }
//        if (ReloadParent)
//            redirectOnSaveAction();
//    }
//    parent.isTstructPopup = false;
//    eval(callParent("removeOverlayFromBody()"));
//    ShowDimmer(false);
//});

//function closeRemodalPopup() {
//    var inst = $('[data-remodal-id=axpertPopupModal]:not(.remodal-is-closed):eq(0)').remodal();
//    try {
//        inst.close();
//    } catch (ex) { }
//}


//function pushValToSession(key, val) {
//    $.ajax({
//        type: "POST",
//        url: "../WebService.asmx/AddSessionPair",
//        cache: false,
//        async: false,
//        contentType: "application/json;charset=utf-8",
//        data: JSON.stringify({ key: key, val: val }),
//        dataType: "json",
//        success: function (data) {
//        },
//    });
//}
//$(document).on('click', '#homeIcon,#dashBoardIcon,.leftPartAC', function () {
//    if (axMenuStyle === "classic") {
//        if (!checkForAxpPopUpExists())
//            $("#dvSelectedGlobalVar", eval(callParent('dvSelectedGlobalVar', 'id'))).show();
//        $("#wrapperForMainNewData", eval(callParent('wrapperForMainNewData', 'id'))).show();

//        //hide utilities menu if user don't have access to any menu (Responsibilities, Import data, Export data, Import history, In-memory DB, Config app, Widget builder)
//        if (visibleAppSettings > 0)
//            $("#ExportImportCogIcon", eval(callParent('ExportImportCogIcon', 'id'))).show();
//    }
//});

//$(document).on('click', '#dashBoardIcon', function () {
//    if (!checkForAxpPopUpExists())
//        $("#dvSelectedGlobalVar", eval(callParent('dvSelectedGlobalVar', 'id'))).show();
//    $("#ExportImportCogIcon", eval(callParent('ExportImportCogIcon', 'id'))).show();
//    $("#wrapperForMainNewData", eval(callParent('wrapperForMainNewData', 'id'))).show();

//});

//$(document).on('click', '.leftPartAC', function () {
//    if (!checkForAxpPopUpExists())
//        $("#dvSelectedGlobalVar", eval(callParent('dvSelectedGlobalVar', 'id'))).show();
//    $("#ExportImportCogIcon", eval(callParent('ExportImportCogIcon', 'id'))).show();
//    $("#wrapperForMainNewData", eval(callParent('wrapperForMainNewData', 'id'))).show();

//});
