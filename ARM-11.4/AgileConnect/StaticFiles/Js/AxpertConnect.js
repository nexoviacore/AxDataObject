var repeatArray = new Array();
var refreshFldsHTML = new Object();
var rownumList = new Object();
var apiCallsCount = 0;
var loadedAPIs = [];
var pendingRowAPIs = [];

var tstGridDataObj = {};
var tstNonGridDataObj = {};
var tstGridDcRowNos = {};
var htmlTagsArray = ['TD', 'DIV', 'SPAN', 'LABEL', 'LI'];

$(document).ready(function () {
    doPageLoadActions();
    addPopupsHTML();
})

function doPageLoadActions() {
    getRefreshFldsHTML();
    loadPageData();
    loadDataFromAPIs();
    bindEvents();
    displayMainLoader("hide");
}

function bindEvents(bindOptions) {
    let $elem = undefined;
    let events = undefined;

    if (typeof bindOptions != "undefined") {
        $elem = bindOptions.elem;
        events = bindOptions.events;
    }

    if (typeof $elem == "undefined") {
        $elem = $(document); //Bind events to existing fields on pageload.
    }
    else {
        $elem = $($elem);
    }

    if (typeof events == "undefined" || events.indexOf(".ax-openpopup") > -1) {
        $elem.find(".ax-openpopup").off('click').click(function () {
            openPopups($(this));
        });
    }

    if (typeof events == "undefined" || events.indexOf(".ax-openinline") > -1) {
        $elem.find(".ax-openinline").off('click').click(function () {
            openInline(this);
        });
    }

    if (typeof events == "undefined" || events.indexOf(".ax-uploadfile") > -1) {
        $elem.find(".ax-uploadfile").off('click').click(function () {
            var file = $(this).attr("data-file-target");
            var filePath = $(this).attr("data-file-path");
            uploadFile({ file: file, filePath: filePath });
        });
    }

    if (typeof events == "undefined" || events.indexOf(".ax-save") > -1) {
        $elem.find(".ax-save").off('click').click(function () {
            let $saveBtn = $(this);
            if ($saveBtn.hasClass('ax-save-disabled')) {
                return;
            }

            $saveBtn.addClass("ax-save-disabled");
            var formId = $saveBtn.attr("data-target");
            var saveDataOptions = {
                formId: formId,
                isAsync: false,
                saveElem: $saveBtn
            };

            saveData(saveDataOptions);
        });
    }

    if (typeof events == "undefined" || events.indexOf(".ax-saveasync") > -1) {
        $elem.find(".ax-saveasync").off('click').click(function () {
            $(this).addClass("ax-save-disabled");            
            var formId = $(this).attr("data-target");
            var saveDataOptions = {
                formId: formId,
                isAsync: true,
                saveElem: $(this)
            };
            saveData(saveDataOptions);
        });
    }

    if (typeof events == "undefined" || events.indexOf(".ax-refreshhtml") > -1) {
        $elem.find(".ax-refreshhtml:not(.ondemand)").off('click').click(function () {
            var elem = $(this);
            refreshHTML(elem);
        });
    }

    if (typeof events == "undefined" || events.indexOf(".ax-copyhtml") > -1) {
        $elem.find(".ax-copyhtml").off('click').click(function () {
            var elem = $(this);
            var sourceId = elem.attr("data-sourceid");
            var sourceHtml = $(elem.attr("data-source")).outerHTML();
            var targetElem = elem.attr("data-target");

            var copyHtmlOptions = {
                sourceId: sourceId,
                sourceHtml: sourceHtml,
                targetElem: targetElem
            };

            copyHTML(copyHtmlOptions);
        });
    }

    if (typeof events == "undefined" || events.indexOf(".ax-removehtml") > -1) {
        $elem.find('.ax-removehtml').off('click').click(function (e) {
            var $elem = $(this);
            let dcRowId = $elem.attr('data-ax-dcrowid') || "0";
            if (dcRowId != "0") {
                let deleteAPI = $elem.attr('data-ax-dcrowdelete');
                if (typeof deleteAPI != "undefined") {
                    confirmAlert({
                        title: "Confirm delete",
                        message: "Do you want to permanently delete the data?",
                        yesCaption: "Confirm",
                        noCaption: "No",
                        yesClick: function () {
                            deleteDcRowFromDB({ formId: deleteAPI, rowid: dcRowId });
                            $($elem.attr("data-target")).remove();
                        }
                    })
                }
            }
            else {
                $($elem.attr("data-target")).remove();
            }

        });
    }

    if (typeof events == "undefined" || events.indexOf(".ax-logout") > -1) {
        $elem.find('.ax-logout').off('click').click(function (e) {
            logout();
        });
    }

    if (typeof events == "undefined" || events.indexOf(".button-plus") > -1) {
        $elem.find('.button-plus').off('click').click(function (e) {
            incrementValue(e);
        });
    }

    if (typeof events == "undefined" || events.indexOf(".button-minus") > -1) {
        $elem.find('.button-minus').off('click').click(function (e) {
            decrementValue(e);
        });

    }

    if (typeof events == "undefined" || events.indexOf(".ax-checklist") > -1) {
        $elem.find('input[type="checkbox"]').off('keypress').keypress(function (e) {
            e.preventDefault();
            if ((e.keyCode ? e.keyCode : e.which) == 13) {
                $(this).trigger('click');
            }
        })
    }

    if (typeof events == "undefined" || events.indexOf(".ax-radiolist") > -1) {
        $elem.find('input[type="radio"]').off('keypress').keypress(function (e) {
            e.preventDefault();
            if ((e.keyCode ? e.keyCode : e.which) == 13) {
                $(this).trigger('click');
            }
        })
    }

    if (typeof events == "undefined" || events.indexOf(".ax-row-lastfld") > -1) {
        $('.ax-row-lastfld').off('keydown');
        $elem.find('.ax-row-lastfld').off('keydown').on("keydown", function (e) {
            let $lastFld = $(this);
            if ((e.keyCode ? e.keyCode : e.which) == 9) {
                if ($($lastFld.attr("data-ax-newrowbtn")).length > 0) {
                    $($lastFld.attr("data-ax-newrowbtn")).click();
                }
            }
        })
    }
}

// increment and decriment
function incrementValue(e) {
    e.preventDefault();
    var fieldName = $(e.target).data('field');
    var parent = $(e.target).closest('div');
    var currentVal = parseInt(parent.find('input[name=' + fieldName + ']').val(), 10);

    if (!isNaN(currentVal)) {
        parent.find('input[name=' + fieldName + ']').val(currentVal + 1);
    } else {
        parent.find('input[name=' + fieldName + ']').val(0);
    }
}

function decrementValue(e) {
    e.preventDefault();
    var fieldName = $(e.target).data('field');
    var parent = $(e.target).closest('div');
    var currentVal = parseInt(parent.find('input[name=' + fieldName + ']').val(), 10);

    if (!isNaN(currentVal) && currentVal > 0) {
        parent.find('input[name=' + fieldName + ']').val(currentVal - 1);
    } else {
        parent.find('input[name=' + fieldName + ']').val(0);
    }
}

function bindControls($elem) {
    $elem = $($elem);

    $elem.find('.ax-magicsearch').addBack('.ax-magicsearch').each(function () {
        let $msElem = $(this);
        let datasource = $msElem.attr("data-ax-datasource");
        let col = $msElem.attr("data-ax-magicsearch-col");

        if (typeof datasource != "undefined" && typeof col != "undefined") {
            pendingRowAPIs.push(datasource);
            $.when(loadDataAsync({
                dataId: datasource
            })).then(function (data, textStatus, jqXHR) {
                let index = pendingRowAPIs.indexOf(datasource);
                if (index > -1) {
                    pendingRowAPIs.splice(index, 1);
                }

                let tempData = dataConvert({ data: data, dataId: datasource });
                createMagicSearch({
                    elem: $msElem,
                    dataSource: tempData,
                    field: col,
                    dataId: datasource
                });
            });
        }
    })

    $elem.find(".ax-data-ondemand:not('.ax-magicsearch')").each(function () {
        let $dataElem = $(this);
        let datasource = $dataElem.attr("data-source");
        if (typeof datasource != "undefined") {
            pendingRowAPIs.push(datasource);
            $.when(loadDataAsync({
                dataId: datasource,
                elem: $dataElem
            })).then(function (data, textStatus, jqXHR) {
                let index = pendingRowAPIs.indexOf(datasource);
                if (index > -1) {
                    pendingRowAPIs.splice(index, 1); 2
                }
            })
        }
    });

    $elem.find(".ax-selectpicker").each(function () {
        let $selectElem = $(this);
        $selectElem.selectpicker('refresh');
    })

    $elem.find(".ax-select2").each(function () {
        let $select2Elem = $(this);
        let dataId = $select2Elem.attr('data-ax-datasource');
        if (typeof dataId != "undefined") {
            let select2Options = { refresh: false, ajaxData: false, dataId: dataId, elem: $select2Elem };
            createSelect2(select2Options);
        }
    })
}

function duplicateCheckEvent(targetElem) {
    $(targetElem + " .ax-non-duplicate:not(div,.ax-duplicate-event-assigned):visible").on("changed.bs.select",
        function (e, clickedIndex, newValue, oldValue) {
            if (this.value == "") return;
            var elem = $(this);
            var currVal = this.value;
            var groupName = $(this).attr("name");
            var isValid = true;
            $(".ax-non-duplicate[name='" + groupName + "']").not(elem).each(function () {
                if ($(this).val() == currVal) {
                    isValid = false;
                    return false;
                }
            })

            if (!isValid) {
                try {
                    ToastMaker("Duplicate value.");

                } catch (ex) {
                    alert("Duplicate value.");

                };

                elem.focus();
            }
        });
    $(targetElem + " .ax-non-duplicate:not(div,.ax-duplicate-event-assigned):visible").addClass("ax-duplicate-event-assigned");
}

function checkDuplicate($targetElem) {
    $targetElem = $($targetElem);
    var duplicateExists = false;
    var arr = [];
    $targetElem.find(".ax-non-duplicate:not(div):visible").each(function () {
        var $elem = $(this);
        var value = $elem.val();
        if (arr.indexOf(value) == -1)
            arr.push(value);
        else {
            var errmsg = $elem.data("duplicate-msg");
            try {
                if (errmsg != "") {
                    ToastMaker(errmsg);
                }
                else {
                    ToastMaker("Duplicate value not allowed.");
                }
                $elem.focus();
                duplicateExists = true;
                return false;

            } catch (ex) {
                alert("Duplicate value not allowed.");
                $elem.focus();
                duplicateExists = true;
                return false;
            };
        }
    });

    return duplicateExists;
}

function checkEmpty($targetElem) {
    $targetElem = $($targetElem);
    var emptyExists = false;
    $targetElem.find(".ax-required:not(div):visible").each(function () {
        var $elem = $(this);
        var value = $elem.val();
        if (value == "") {
            var errmsg = $elem.data("validation-msg");
            try {
                if (errmsg != "") {
                    ToastMaker(errmsg);
                }
                else {
                    ToastMaker("Empty value not allowed.");
                }
                $elem.focus();
                emptyExists = true;
                return false;

            } catch (ex) {
                alert("Empty value not allowed.");
                $elem.focus();
                emptyExists = true;
                return false;
            };
        }
    });

    return emptyExists;
}

function openInline(currElem) {
    var pageId = $(currElem).attr("data-target");
    var targetElem = $(currElem).attr("data-elem");
    if (targetElem == "#divHome")
        displayMainLoader("show");

    setMobileViewFlag();
    if (typeof localStorage.getItem('RCP_MobileView') != "undefined" && localStorage.getItem('RCP_MobileView') == "true") {
        var pageIdMobile = $(currElem).attr("data-target-mobile");
        if (typeof pageIdMobile != "undefined")
            pageId = pageIdMobile;
    }

    let loadHTMLOptions = {
        pageId: pageId,
        target: targetElem,
        currElem: currElem
    }

    loadHTML(loadHTMLOptions);
}

function refreshHTML($elem) {
    $elem = $($elem);
    var fldId = $elem.attr("data-target");
    var refresh = $elem.attr("data-refresh");
    if (typeof refresh != "undefined") {
        refreshHTMLElement(fldId, (refresh.toLowerCase() == "true" ? true : false));
    }
    else
        refreshHTMLElement(fldId, false);
}

function refreshHTMLElement(fldId, refresh) {
    var fld = $("#" + fldId);
    if (fld.length > 0) {
        fld.replaceWith(refreshFldsHTML[fldId]);
        $("#" + fldId + " .ax-data," + "#" + fldId + " .ax-data-ondemand").each(function () {
            var datasource = $(this).attr("data-source");
            var showloader = $(this).attr("data-show-loader");
            if (typeof showloader == "undefined") {
                showloader = true;
            }
            if (typeof datasource != "undefined") {
                loadData({
                    dataId: datasource,
                    elem: $(this),
                    refresh: refresh,
                    showLoader: showloader
                });
            }
        });
    }
}

function loadPageData() {
    $(".ax-pagedata:not(.ax-repeat,.ax-repeat-innerhtml)").each(function () {
        doLoadValues($(this), $(this).attr("data-source"));
    });

    $(".ax-repeat.ax-pagedata:not(.ax-data-ondemand)").each(function () {
        repeatArray.push($(this).attr("data-source"));
    });

    for (var i = 0; i < repeatArray.length; i++) {
        $(".ax-repeat.ax-pagedata[data-source='" + repeatArray[i] + "']:not(.ax-ondemand)").each(function () {
            doRepeaterLoad($(this), $(this).attr("data-source"));
        });
    }

    $(".ax-repeat-innerhtml.ax-pagedata").each(function () {
        doRepeaterLoad($(this), $(this).attr("data-source"));
    });
}

function getRefreshFldsHTML() {
    $(".ax-refreshhtml").each(function () {
        let fldId = $(this).attr("data-target");
        if (typeof refreshFldsHTML[fldId] == 'undefined') {
            refreshFldsHTML[fldId] = $("#" + fldId).outerHTML();
        }
    });
}

//Plugin to get complete HTML
jQuery.fn.outerHTML = function () {
    return (this[0]) ? this[0].outerHTML : '';
};

function doLoadValues(elem, dataObj) {
    dataObj = eval(dataObj);
    var elemHtml = elem.outerHTML();

    if (elemHtml.indexOf("{{") > -1 || elemHtml.indexOf("<axdata>") > -1) {
        for (var key in dataObj[0]) {
            if (dataObj[0].hasOwnProperty(key) && (elemHtml.indexOf("{{" + key + "}}") > -1 || elemHtml.indexOf("<axdata>" + key + "</axdata>"))) {
                var val = dataObj[0][key];
                elemHtml = elemHtml.replaceAll("{{" + key + "}}", val).replaceAll("<axdata>" + key + "</axdata>", val);
            }
        }
    }

    elem.replaceWith(elemHtml);
}

function doRepeaterLoad(elem, dataObj) {
    dataObj = eval(dataObj);

    var elemHtml = "";
    if (elem.hasClass("ax-repeat-innerhtml")) {
        elemHtml = elem.html();
    }
    else
        elemHtml = elem.outerHTML();

    var tempHtml = elemHtml;
    var finalHtml = "";

    for (var i = 0; i < dataObj.length; i++) {
        if (typeof elem.attr("data-filter") != "undefined") {
            var filter = elem.attr("data-filter");
            filter = filter.split('=');
            if (dataObj[i][filter[0]] == filter[1] && (tempHtml.indexOf("{{") > -1 || tempHtml.indexOf("<axdata>") > -1)) {
                for (var key in dataObj[i]) {
                    if (dataObj[i].hasOwnProperty(key) && (tempHtml.indexOf("{{" + key + "}}") > -1 || tempHtml.indexOf("<axdata>" + key + "</axdata>") > -1)) {
                        var val = dataObj[i][key];
                        tempHtml = tempHtml.replaceAll("{{" + key + "}}", val).replaceAll("<axdata>" + key + "</axdata>", val);
                    }
                }

                finalHtml += tempHtml;
                tempHtml = elemHtml;
                break;
            }
        }
        else {
            for (var key in dataObj[i]) {
                if (dataObj[i].hasOwnProperty(key) && (tempHtml.indexOf("{{" + key + "}}") > -1 || tempHtml.indexOf("<axdata>" + key + "</axdata>") > -1)) {
                    var val = dataObj[i][key];
                    tempHtml = tempHtml.replaceAll("{{" + key + "}}", val).replaceAll("<axdata>" + key + "</axdata>", val);
                }
            }

            finalHtml += tempHtml;
            tempHtml = elemHtml;
        }

    }

    if (elem.hasClass("ax-repeat-innerhtml")) {
        elem.html(finalHtml);
    }
    else
        elem.replaceWith(finalHtml);
}

function loadHTML(loadHTMLOptions) {
    displayLoader("show");

    try {
        loadHTMLOptions = generateLoadHTMLOptions(loadHTMLOptions);
    }
    catch (ex) { }

    if (typeof loadHTMLOptions.beforeLoad != "undefined") {
        loadHTMLOptions = loadHTMLOptions.beforeLoad(loadHTMLOptions);
    }

    $.ajax({
        type: "GET",
        url: "../pages/" + loadHTMLOptions.pageId,
        headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
        cache: false,
        async: true,
        dataType: "html",
        success: function (data) {
            loadHTMLOptions.data = data;
            loadHTML_Success(loadHTMLOptions);
        },
        error: function (request, status, error) {
            displayLoader("hide");
            console.log(request);
            alert(request.responseText);
        }
    });
}

function loadHTML_Success(loadHTMLOptions) {
    var targetElem = $(loadHTMLOptions.target);
    if (targetElem.length > 0) {
        if (typeof $(loadHTMLOptions.currElem).attr("data-target-params") != "undefined") {
            var targetPageParams = $(loadHTMLOptions.currElem).attr("data-target-params");
            try {
                targetPageParams = beforePageParams(loadHTMLOptions.pageId, loadHTMLOptions.target, loadHTMLOptions.currElem, targetPageParams);
            }
            catch (ex) { }
            targetElem.attr("data-params", targetPageParams);
        }

        targetElem.html(loadHTMLOptions.data);

    }

    if (typeof loadHTMLOptions.afterLoad != "undefined") {
        loadHTMLOptions.afterLoad(loadHTMLOptions);
    }

    displayLoader("hide");
}

function loadDataFromAPIs(pageId) {
    if (typeof pageId == 'undefined') {
        pageId = '';
    }
    else {
        pageId = "#" + pageId
    }

    $(pageId + " .ax-data").each(function () {
        let $dataElem = $(this);
        var datasource = $dataElem.attr("data-source");
        if ($dataElem.hasClass('ax-magicsearch')) {
            datasource = $dataElem.attr("data-ax-datasource");
        }
        if (typeof datasource != "undefined") {
            loadData({
                dataId: datasource,
                pageId: pageId,
                elem: $dataElem,
                refresh: false,
                showLoader: true
            });
        }

    });

}

function loadData(dataOptions) { //dataId, pageId, elem, refresh
    try {
        dataOptions = generateLoadDataOptions(dataOptions);
    } catch (ex) { };

    dataOptions.dataObj = new Object();
    dataOptions.dataObj.key = dataOptions.dataId;
    dataOptions.dataObj.refresh = dataOptions.refresh;

    dataOptions.dataObj.dataParams = new Object();

    if (dataOptions.showLoader == true) {
        console.log(dataOptions.dataId)
        displayLoader("show");
    }

    if (typeof dataOptions.beforeLoad != "undefined") {
        dataOptions = dataOptions.beforeLoad(dataOptions);
    }

    $.ajax({
        type: "POST",
        url: "../api/GetData?" + dataOptions.dataId,
        headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
        cache: false,
        contentType: "application/json;charset=utf-8",
        data: JSON.stringify(dataOptions.dataObj),
        async: true,
        dataType: "json",
        success: function (data) {
            dataOptions.data = data;
            try {
                data = dataConvert(dataOptions);
            }
            catch (ex) { }

            loadData_Success(dataOptions.dataId, data, dataOptions.pageId, dataOptions.elem, dataOptions);
        },
        error: function (request, status, error) {
            displayLoader("hide");

            console.log(request);
            alert(request.responseText);
        }
    });
}

async function loadDataAsync(dataOptions) { //dataId, pageId, elem, refresh
    try {
        dataOptions = generateLoadDataOptions(dataOptions);
    } catch (ex) { };

    dataOptions.dataObj = new Object();
    dataOptions.dataObj.key = dataOptions.dataId;
    dataOptions.dataObj.refresh = dataOptions.refresh;

    dataOptions.dataObj.dataParams = new Object();

    if (dataOptions.showLoader == true) {
        console.log(dataOptions.dataId)
        displayLoader("show");
    }

    if (typeof dataOptions.beforeLoad != "undefined") {
        dataOptions = dataOptions.beforeLoad(dataOptions);
    }

    return $.ajax({
        type: "POST",
        url: "../api/GetData?" + dataOptions.dataId,
        headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
        cache: false,
        contentType: "application/json;charset=utf-8",
        data: JSON.stringify(dataOptions.dataObj),
        async: true,
        dataType: "json",
        success: function (data) {
            dataOptions.data = data;
            try {
                data = dataConvert(dataOptions);
            }
            catch (ex) { }

            loadData_Success(dataOptions.dataId, data, dataOptions.pageId, dataOptions.elem, dataOptions);
        },
        error: function (request, status, error) {
            displayLoader("hide");

            console.log(request);
            alert(request.responseText);
        }
    });
}

function loadData_Success(dataId, data, pageId, elem, dataOptions) {
    if (typeof elem != "undefined") {
        if (typeof data != "undefined" && data != "") {
            if (elem.hasClass('ax-magicsearch')) {
                let col = elem.attr("data-ax-magicsearch-col");
                createMagicSearch({
                    elem: elem,
                    dataSource: data,
                    field: col,
                    dataId: dataId
                });
            }
            else
                doRepeaterLoad(elem, data);
        }
    }

    if (typeof dataOptions.afterLoad != "undefined")
        dataOptions.afterLoad(data, dataOptions);

    if (typeof dataOptions.setValue != "undefined")
        dataOptions.setValue(data, dataOptions);

    loadedAPIs.push(dataOptions.dataId);

    displayLoader("hide");
}

function loadFilteredData(dataOptions) { //dataId, pageId, elem, refresh
    try {
        dataOptions = generateLoadDataOptions(dataOptions);
    } catch (ex) { };

    dataOptions.dataObj = new Object();
    dataOptions.dataObj.key = dataOptions.dataId;
    dataOptions.dataObj.refresh = dataOptions.refresh;

    dataOptions.dataObj.dataParams = new Object();

    if (dataOptions.showLoader == true) {
        console.log(dataOptions.dataId)
        displayLoader("show");
    }

    if (typeof dataOptions.beforeLoad != "undefined") {
        dataOptions = dataOptions.beforeLoad(dataOptions);
    }
    dataOptions.isFilteredData = true;
    $.ajax({
        type: "POST",
        url: "../api/GetFilteredData?" + dataOptions.dataId,
        headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
        cache: false,
        contentType: "application/json;charset=utf-8",
        data: JSON.stringify(dataOptions.dataObj),
        async: true,
        dataType: "json",
        success: function (data) {
            dataOptions.data = data;
            try {
                data = dataConvert(dataOptions);
            }
            catch (ex) { }

            loadData_Success(dataOptions.dataId, data, dataOptions.pageId, dataOptions.elem, dataOptions);
        },
        error: function (request, status, error) {
            displayLoader("hide");

            console.log(request);
            alert(request.responseText);
        }
    });
}

function saveData(saveDataOptions) {
    try {
        saveDataOptions = generateSaveDataOptions(saveDataOptions);
    } catch (ex) { };

    if (validateSaveData(saveDataOptions.formId) == false) {
        $(saveDataOptions.saveElem).removeClass("ax-save-disabled");
        return;//Default Save Validation failed
    }

    if (typeof saveDataOptions.validateSave != "undefined" && saveDataOptions.validateSave() == false) {
        $(saveDataOptions.saveElem).removeClass("ax-save-disabled");
        return; //Custom Save Validation failed
    }

    var saveObj = new Object();
    saveObj.formId = saveDataOptions.formId;

    if (typeof saveDataOptions.userId != "undefined")
        saveObj.userId = saveDataOptions.isAsync;
    else
        saveObj.userId = localStorage["AxpertConnectUser"];

    if (typeof saveDataOptions.isAsync != "undefined")
        saveObj.isAsync = saveDataOptions.isAsync;
    else
        saveObj.isAsync = false;

    if (typeof saveDataOptions.beforeSave != "undefined")
        saveDataOptions.beforeSave(saveDataOptions);

    saveObj.saveJson = {};
    saveObj.saveJson.recdata = getTstructSaveData(saveDataOptions.formId);

    if (typeof saveDataOptions.saveJson != "undefined")
        saveObj.saveJson = saveDataOptions.saveJson(saveObj.saveJson);

    if ($("#recordid").length > 0) {
        saveObj.saveJson.recordid = $("#recordid").val();
    }

    if (saveObj.isAsync) {
        callSaveDataToRMQ(saveDataOptions, saveObj);
    } else {
        callSaveDataToDB(saveDataOptions, saveObj);
    }
}

function generateSaveJSON(formId) {
    var saveJson = {};

    if (typeof formId == 'undefined') {
        formId = '';
    } else if (formId.indexOf("#") == -1) {
        formId = "#" + formId
    }

    $(formId + ' .ax-savefld').each(function () {
        var $elem = $(this);
        saveJson[$elem.attr("data-ax-fld")] = getFieldValue($elem);
    });

    return saveJson;
}

function callSaveDataToDB(saveDataOptions, saveObj) {

    $.ajax({
        type: "POST",
        url: "../api/SaveToDB",
        headers: {
            "Authorization": localStorage.getItem('RCP_JwtToken')
        },
        cache: false,
        contentType: "application/json;charset=utf-8",
        data: JSON.stringify(saveObj),
        async: true,
        dataType: "json",
        success: function (data) {
            $(saveDataOptions.saveElem).removeClass("ax-save-disabled");
            saveData_Success(data, saveDataOptions, saveObj);

            if (typeof saveDataOptions.onSaveSuccess != "undefined")
                saveDataOptions.onSaveSuccess(data);
        },
        error: function (request, status, error) {
            $(saveDataOptions.saveElem).removeClass("ax-save-disabled");
            console.log(request);

            if (typeof saveDataOptions.onSaveError != "undefined")
                saveDataOptions.onSaveError(request, status, error);
        }
    });
}

function callSaveDataToRMQ(saveDataOptions, saveObj) {

    $.ajax({
        type: "POST",
        url: "../api/SaveToRMQ",
        headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
        cache: false,
        contentType: "application/json;charset=utf-8",
        data: JSON.stringify(saveObj),
        async: true,
        dataType: "text",
        success: function (data) {
            $(saveDataOptions.saveElem).removeClass("ax-save-disabled");
            saveData_Success(data, saveDataOptions, saveObj);

            if (typeof saveDataOptions.onSaveSuccess != "undefined")
                saveDataOptions.onSaveSuccess(data);
        },
        error: function (request, status, error) {
            console.log(request);
            $(saveDataOptions.saveElem).removeClass("ax-save-disabled");
            if (typeof saveDataOptions.onSaveError != "undefined")
                saveDataOptions.onSaveError(request, status, error);
        }
    });
}

function saveData_Success(data, saveDataOptions, saveObj) {
    try {
        console.log(data);
    } catch (ex) { };

    let isSaveSuccess = true;
    try {
        var errorNode = data.result[0].error;
        if (typeof errorNode != "undefined") {
            isSaveSuccess = false;
            ToastMaker(errorNode.msg.toString());
        }
    }
    catch (ex) {
    }

    if (isSaveSuccess && typeof saveDataOptions.afterSave != "undefined")
        saveDataOptions.afterSave(data);
}

function validateSaveData(formId) {

    if (typeof formId == 'undefined') {
        formId = '';
    } else if (formId.indexOf("#") == -1) {
        formId = "#" + formId
    }

    if (checkEmpty(formId) || checkDuplicate(formId)) {
        return false;
    }
    else
        return true;
}

function dataConvert(dataOptions) {
    try {
        var errorNode = dataOptions.data.result[0].error;
        if (typeof errorNode != "undefined") {
            console.log("API Error: " + dataOptions.dataId + "-" + errorNode.msg.toString());
            ToastMaker("API Error: " + dataOptions.dataId + "-" + errorNode.msg.toString());
            return;
        }
    }
    catch (ex) {
        //ToastMaker("Error occurred. Please try again.");
    }

    switch (dataOptions.type) {
        case "Axpert-GetChoice":
            try {
                if (typeof dataOptions.data.result[0].result.row != "undefined") {
                    return dataOptions.data.result[0].result.row;
                }
            }
            catch (ex) { };
            break;
        default:
            {
                try {
                    if (typeof dataOptions.data.result[0].result.row != "undefined") {
                        return dataOptions.data.result[0].result.row;
                    }
                }
                catch (ex) { };

                try {
                    if (typeof dataOptions.data.result[0].result != "undefined") {
                        return dataOptions.data.result[0].result;
                    }
                }
                catch (ex) { };
            }
    }

    return dataOptions.data;
}

var toBase64 = file => new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = () => resolve(reader.result.split(',')[1]);
    reader.onerror = error => reject(error);
});

async function uploadFile(fileUploadOptions) {

    let file = document.querySelector("#" + fileUploadOptions.file).files[0];
    var fileBase64 = await toBase64(file);
    var fileDef = new Object();

    fileUploadOptions.file = file;

    try {
        fileUploadOptions = generateFileUploadOptions(fileUploadOptions);
    } catch (ex) { };

    if (typeof fileUploadOptions.beforeUpload != "undefined")
        fileUploadOptions.beforeUpload();

    fileDef.fileName = fileUploadOptions.fileName;
    fileDef.fileBase64 = fileBase64;
    fileDef.filePath = fileUploadOptions.filePath;

    $.ajax({
        type: "POST",
        url: "../api/UploadFile",
        headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
        cache: false,
        contentType: "application/json;charset=utf-8",
        data: JSON.stringify(fileDef),
        async: true,
        dataType: "text",
        success: function (data) {
            uploadFile_Success(data, fileUploadOptions);
        },
        error: function (request, status, error) {
            console.log(request);
            alert(request.responseText);
        }
    });
}

function downloadFile(file, filePath) {
    var fileDef = new Object();
    fileDef.fileName = file.name;
    fileDef.filePath = filePath;

    $.ajax({
        type: "POST",
        url: "../api/DownloadFile",
        headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
        cache: false,
        contentType: "application/json;charset=utf-8",
        data: JSON.stringify(fileDef),
        async: true,
        dataType: "text",
        success: function (data) {
            downloadFile_Success(data);
        },
        error: function (request, status, error) {
            console.log(request);
            alert(request.responseText);
        }
    });
}

function uploadFile_Success(data, fileUploadOptions) {
    if (typeof fileUploadOptions.afterUpload != "undefined")
        fileUploadOptions.afterUpload();
}

function downloadFile_Success(data) {

}

Date.prototype.toDateFormat = function (format) {
    if (format.toLowerCase() == "dd-mmm-yyyy") {
        let monthNames = ["Jan", "Feb", "Mar", "Apr",
            "May", "Jun", "Jul", "Aug",
            "Sep", "Oct", "Nov", "Dec"];

        let day = this.getDate();

        let monthIndex = this.getMonth();
        let monthName = monthNames[monthIndex];

        let year = this.getFullYear();

        return `${day}-${monthName}-${year}`;
    }
}

function copyHTML(copyHtmlOptions) {
    try {
        copyHtmlOptions = generateCopyHtmlOptions(copyHtmlOptions);
    } catch (ex) { };

    if (typeof rownumList[copyHtmlOptions.sourceId] != "undefined") {
        copyHtmlOptions.sourceHtml = copyHtmlOptions.sourceHtml.replaceAll("ax-rownum", rownumList[copyHtmlOptions.sourceId].toString()).replaceAll("ax-tempgridrow", "");
    }
    else {
        rownumList[copyHtmlOptions.sourceId] = 1;
        copyHtmlOptions.sourceHtml = copyHtmlOptions.sourceHtml.replaceAll("ax-rownum", rownumList[copyHtmlOptions.sourceId].toString()).replaceAll("ax-tempgridrow", "");
    }

    var $newElem = $(copyHtmlOptions.sourceHtml);
    copyHtmlOptions.newElem = $newElem;
    copyHtmlOptions.newRowNo = rownumList[copyHtmlOptions.sourceId]

    bindControls($newElem);

    let rowNum = rownumList[copyHtmlOptions.sourceId];
    if ($newElem.hasClass('ax-dcrow')) {
        let dcNo = $(copyHtmlOptions.targetElem).closest('.ax-dc').attr('data-ax-dcno')
        tstGridDcRowNos['DC' + dcNo] = tstGridDcRowNos['DC' + dcNo] + 1 || 1;
        rowNum = tstGridDcRowNos['DC' + dcNo];
    }

    $.when(isRowAPIsLoaded()).then(function () {
        bindEvents({ elem: $newElem });
        if (typeof copyHtmlOptions.rowData != "undefined") {
            let rowData = copyHtmlOptions.rowData;
            bindDcRowData($newElem, rowData);

            if (typeof rowData.dcrowno == "undefined") {
                $newElem.attr('data-ax-dcrowno', rowNum);
            }
        }
        else {
            $newElem.attr('data-ax-dcrowno', rowNum);
        }

        $(copyHtmlOptions.targetElem).append($newElem);

        //try {
        //    afterCopyHTML(copyHtmlOptions.sourceId, copyHtmlOptions.sourceHtml, copyHtmlOptions.targetElem, rownumList[copyHtmlOptions.sourceId]);
        //}
        //catch (ex) { };

        if (typeof copyHtmlOptions.afterCopy != "undefined") {
            copyHtmlOptions.afterCopy(copyHtmlOptions);
        }

        try {
            $(copyHtmlOptions.newElem).find('input,select:visible').filter(':first').focus();
        }
        catch (ex) { }
    });

    rownumList[copyHtmlOptions.sourceId] = rownumList[copyHtmlOptions.sourceId] + 1;
}

function logout() {
    localStorage.removeItem("RCP_JwtToken");
    window.location.href = "login";
}

function setMobileViewFlag() {
    if ($(window).width() < 450) {
        localStorage.setItem("RCP_MobileView", "true");
    }
    else {
        localStorage.setItem("RCP_MobileView", "false");
    }
}

function displayLoader(option) {
    if (option == "show") {
        apiCallsCount++;
        if ($("body .bgloader:not(.mainloader)").length == 0) {
            $("body").append('<div class="d-flex justify-content-center bgloader" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; opacity: 1; background-color: #e1e1e1;z-index:99999;"><div class="spinner-border text-danger" style="margin-top: 30%;" role="status"><span class="visually-hidden"></span></div></div>');
        }
    }
    else if (option == "hide") {
        apiCallsCount--;
        if (apiCallsCount <= 0) {
            $("body .bgloader:not(.mainloader)").fadeOut(100, function () {
                $("body .bgloader:not(.mainloader)").remove();
            });
        }
    }
}

function displayMainLoader(option) {
    if (option == "show") {
        $("#divHome").parent().append('<div class="d-flex justify-content-center bgloader mainloader" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; opacity: 1; background-color: #e1e1e1;z-index:99999;"><div class="spinner-border text-danger" style="margin-top: 30%;" role="status"><span class="visually-hidden"></span></div></div>');
    }
    else if (option == "hide") {
        $("body .bgloader.mainloader").fadeOut(100, function () {
            $("body .bgloader.mainloader").remove();
        });
    }
}

function addPopupsHTML() {
    if ($("#confirmPopup").length == 0) {
        $("#divHome").parent().append(`<div class="modal fade" id="confirmPopup" tabindex="-1" role="dialog"
                    aria-labelledby="exampleModalCenterTitle" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered" role="document">
                        <div class="modal-content rounded-button border-0">
                            <div class="modal-header  primaryBgColor text-light p-1 px-2">
                                <h5 class="modal-title ms-3" id="confirmPopupTitle">Confirmation Title</h5>
                                <button type="button" class="border-0 bg-transparent" class="close" data-bs-dismiss="modal"
                                    aria-label="Close">
                                    <span class="btn btn-danger btn-sm cancelBorder  px-2 py-1" aria-hidden="true">&times;</span>
                                </button>
                            </div>
                            <div class="modal-body">
                                <p class="mb-0" id="confirmPopupMsg">Confirmation Message</p>
                            </div>
                            <div class="modal-footer justify-content-center p-1">
                                <button type="button" class="btn btn-sm btn-grayModal" id="confirmPopupYes" data-confirm-id="" data-bs-dismiss="modal" >Proceed</button>
                                <button type="button" class="btn btn-sm btn-grayModal" id="confirmPopupNo" data-bs-dismiss="modal" >Cancel</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>`);

        $("#confirmPopup").off('hidden.bs.modal').on('hidden.bs.modal', function (e) {
            $("#confirmPopup #confirmPopupTitle").html("Confirmation Title");
            $("#confirmPopup #confirmPopupMsg").html("Confirmation Message");
            $("#confirmPopup #confirmPopupYes").html("Proceed");
            $("#confirmPopup #confirmPopupNo").html("Cancel");
            $("#confirmPopup #confirmPopupYes").attr("data-confirm-id", "");
            $("#confirmPopup #confirmPopupYes").off('click');
            $("#confirmPopup #confirmPopupNo").off('click');
            $("#confirmPopup").off('hide.bs.modal').off('show.bs.modal');
        })
    }

    if ($("#messagePopup").length == 0) {
        $("#divHome").parent().append(`<div class="modal fade" id="messagePopup" tabindex="-1" role="dialog"
                    aria-labelledby="exampleModalCenterTitle" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered" role="document">
                        <div class="modal-content rounded-button border-0">
                            <div class="modal-header  primaryBgColor text-light p-1 px-2">
                                <h5 class="modal-title ms-3" id="messagePopupTitle">Message Title</h5>
                                <button type="button" class="border-0 bg-transparent" class="close" data-bs-dismiss="modal"
                                    aria-label="Close">
                                    <span class="btn btn-danger btn-sm cancelBorder  px-2 py-1" aria-hidden="true">&times;</span>
                                </button>
                            </div>
                            <div class="modal-body">
                                <p class="mb-0" id="messagePopupMsg">Message</p>
                            </div>
                            <div class="modal-footer justify-content-center p-1">
                                <button type="button"  data-bs-dismiss="modal" class="btn btn-sm btn-grayModal" id="messagePopupOk">Ok</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>`);

        $("#messagePopup").on('hidden.bs.modal', function (e) {
            $("#messagePopup #messagePopupTitle").html("Message Title");
            $("#messagePopup #messagePopupMsg").html("Message");
            $("#messagePopup").off('hide.bs.modal').off('show.bs.modal');
            $("#messagePopup #messagePopupOk").off('click');
        })
    }
}

function confirmAlert(confirmOptions) {
    if (confirmOptions.hide == true) {
        $("#confirmPopup").modal("hide");
    }
    else {
        if (typeof confirmOptions.title != "undefined") $("#confirmPopup #confirmPopupTitle").html(confirmOptions.title);
        if (typeof confirmOptions.message != "undefined") $("#confirmPopup #confirmPopupMsg").html(confirmOptions.message);
        if (typeof confirmOptions.yesCaption != "undefined") $("#confirmPopup #confirmPopupYes").html(confirmOptions.yesCaption);
        if (typeof confirmOptions.noCaption != "undefined") $("#confirmPopup #confirmPopupNo").html(confirmOptions.noCaption);
        if (typeof confirmOptions.id != "undefined") $("#confirmPopup #confirmPopupYes").attr("data-confirm-id", confirmOptions.id);

        if (typeof confirmOptions.onload != "undefined") {
            $("#confirmPopup").off('show.bs.modal').on('show.bs.modal', confirmOptions.onload);
        }

        if (typeof confirmOptions.onclose != "undefined") {
            $("#confirmPopup").off('hide.bs.modal').on('hide.bs.modal', confirmOptions.onclose);
        };

        if (typeof confirmOptions.yesClick != "undefined") {
            $("#confirmPopup #confirmPopupYes").off('click').click(confirmOptions.yesClick);
        }

        if (typeof confirmOptions.noClick != "undefined") {
            $("#confirmPopup #confirmPopupNo").off('click').click(confirmOptions.noClick);
        };

        $("#confirmPopup").modal("show");
    }
}

function messageAlert(messageOptions) {
    if (messageOptions.hide == true) {
        $("#messagePopup").modal("hide");
    }
    else {
        if (typeof messageOptions.title != "undefined") $("#messagePopup #messagePopupTitle").html(messageOptions.title);
        if (typeof messageOptions.message != "undefined") $("#messagePopup #messagePopupMsg").html(messageOptions.message);

        if (typeof messageOptions.onload != "undefined") {
            $("#messagePopup").off('show.bs.modal').on('show.bs.modal', messageOptions.onload);
        }

        if (typeof messageOptions.onclose != "undefined") {
            $("#messagePopup").off('hide.bs.modal').on('hide.bs.modal', messageOptions.onclose);
        };

        if (typeof messageOptions.okClick != "undefined") {
            $("#messagePopup #messagePopupOk").off('click').click(messageOptions.okClick);
        }

        $("#messagePopup").modal("show");
    }
}

function generateLoadTstructDataObject(loadJson) {
    var dataObj = {};
    var dcNode;
    for (var i = 0; i < loadJson.length; i++) {
        if (loadJson[i].t == 'dc') {
            dataObj[loadJson[i].n] = {};
            dcNode = loadJson[i].n;
        }
        else {
            if (typeof dataObj[dcNode][loadJson[i].r] == "undefined") {
                dataObj[dcNode][loadJson[i].r] = {};
            }
            let nodeName = loadJson[i].n;
            if (loadJson[i].n.indexOf('axp_recid') > -1) {
                nodeName = 'dcrowid';
            }

            dataObj[dcNode][loadJson[i].r][nodeName] = loadJson[i].v;
            dataObj[dcNode][loadJson[i].r][loadJson[i].n] = loadJson[i].v;
            if (typeof dataObj[dcNode][loadJson[i].r]["dcrowno"] == "undefined") {
                dataObj[dcNode][loadJson[i].r]["dcrowno"] = loadJson[i].r;
                tstGridDcRowNos[dcNode] = parseInt(loadJson[i].r);
            }
        }
    }
    return dataObj;
}

function setFieldValue(fldOptions) {
    if (typeof fldOptions.value != "undefined") {
        let $fld = $(fldOptions.field);
        let value = fldOptions.value;

        if ($fld.hasClass('ax-magicsearch')) {
            $fld.val(value);
        }
        else if ($fld.hasClass('ax-checklist')) {
            value.split(',').forEach(function (item, index) {
                $fld.find('input[type=checkbox][value="' + item + '"]').prop("checked", "checked");
            });
        }
        else if ($fld.hasClass('ax-radiolist')) {
            $fld.find('input[type=radio][value="' + value + '"]').prop("checked", "checked");
        }
        //else if ($fld.hasClass('ax-select2-nongrid')) {
        //    let valArr = value.split(',');

        //    for (let val in valArr) {
        //        let rowData = valArr[val];

        //        if ($elem.find("option[value='" + rowData[idCol] + "']").length > 0) {
        //            $elem.find("option[value='" + rowData[idCol] + "']").each(function () {
        //                $(this).attr('data-ax-dcrowid', rowData.dcrowid).attr('data-ax-dcrowno', rowData.dcrowno)
        //            });
        //            valArr.push(rowData[idCol]);
        //        } else {
        //            $elem.append(getSelectedOption({ id: rowData[idCol], text: rowData[textCol], dcrowid: rowData.dcrowid, dcrowno: rowData.dcrowno }));
        //            valArr.push(rowData[idCol]);
        //        }
        //    }

        //    $fld.val(valArr).trigger('change');
        //}
        else {
            let fldType = $fld.attr("type");
            if (fldType == 'radio' || fldType == 'checkbox') {
                let tempVal = value.toString().toUpperCase();
                if (tempVal == 'T' || tempVal == 'YES' || tempVal == 'TRUE') {
                    $fld.prop('checked', 'checked')
                }
                else {
                    if ($fld.val() == value) {
                        $fld.prop('checked', 'checked')

                    }
                }

            }
            else {
                if (htmlTagsArray.indexOf($fld.prop("tagName").toString().toUpperCase()) > -1) {
                    $fld.html(value)

                }
            }

            try {
                $fld.val(value);
            }
            catch (ex) { }
        }
    }

}

function getFieldValue($fld) {    
    let fldVal = "";

    if ($fld.hasClass('ax-checklist')) {
        let valueList = [];
        $fld.find('input[type=checkbox]:checked').each(function () {
            valueList.push($(this).val());
        })

        fldVal = valueList.join(',');
    }
    else if ($fld.hasClass('ax-radiolist')) {
        fldVal = $fld.find('input[type=radio]:checked').val()
    }
    else if ($fld.hasClass('ax-select2-nongrid')) {        
        fldVal = $fld.val().join(',');
    }
    else if ($fld.hasClass('ax-select2-grid')) {
        fldVal = $fld.select2('data');
    }
    else {
        let fldType = $fld.attr("type");
        if (fldType == 'radio' || fldType == 'checkbox') {
            if ($fld.prop('checked') == true || $fld.prop('checked') == 'checked') {
                fldVal = $fld.val();
            }
        }
        else {
            try {
                fldVal = $fld.val();
            }
            catch (ex) { }
        }
    }

    if (typeof fldVal == "undefined")
        fldVal = '';

    return fldVal;

}

function createMagicSearch(fldOptions) {
    let $elem = fldOptions.elem;
    $elem.each(function () {
        let currVal = $elem.val();
        let searchOptions = {};
        try {
            searchOptions = generateSearchOptions(fldOptions);
        } catch (ex) { };

        $(this).magicsearch({
            dataSource: fldOptions.dataSource,
            fields: [].push(fldOptions.field),
            id: fldOptions.field,
            format: '%' + fldOptions.field + '%',
            dropdownBtn: true,
            noResult: 'No matching data',
            focusShow: true,
            isClear: false,
            success: function ($input, data) {
                if (typeof searchOptions.onChange != "undefined") {
                    searchOptions.onChange($input, data);
                }
                return true;
            },
            afterDelete: function ($input, data) {
                if (typeof searchOptions.onClear != "undefined") {
                    searchOptions.onClear($input, data);
                }
                return true;
            }
        }).addClass('searchConfigured').on("keyup", function (event) {
            if (event.key === "Enter") {
                event.preventDefault();
                $elem.parent().find('.magicsearch-arrow').click();
            }
        });

        $(this).val(currVal);
    });
}

function until(conditionFunction) {
    const poll = resolve => {
        if (conditionFunction()) resolve();
        else setTimeout(_ => poll(resolve), 200);
    }

    return new Promise(poll);
}

async function isAPIDataLoaded(apiId) {
    await until(_ => (loadedAPIs.indexOf(apiId) > -1) == true);
    return true;
}

async function isRowAPIsLoaded() {
    await until(_ => (pendingRowAPIs.length == 0) == true);
    return true;
}

async function loadMultipleDataAync(asyncCallList, callback) {
    try {
        const results = await Promise.all(asyncCallList);
        callback(results);
    } catch (ex) { }
}


function getParamsObj(paramStr) {
    let paramObj = {};
    let paramArr = paramStr.split('&');
    for (var i = 0; i < paramArr.length; i++) {
        let tempParam = paramArr[i].split('=');
        if (tempParam.length == 2)
            paramObj[tempParam[0]] = tempParam[1];
    }

    return paramObj;
}

function openPopups(elem) {
    elem = $(elem);
    let popUpOptions = {
        title: elem.attr('data-popup-title') || '',
        iframeUrl: elem.attr('data-popup-iframeUrl') || '',
        iframeClass: elem.attr('data-popup-iframeClass') || ''
    }

    let paramStr = elem.attr('data-target-params');
    let paramsName = elem.attr('data-target-params-name');
    if (typeof paramStr != "undefined" && typeof paramStr != "undefined") {
        pageParams[paramsName] = new Object();
        pageParams[paramsName] = getParamsObj(paramStr);
    }

    $axPopupModal = $("#axPopupModal");
    $axPopupModal.find("#axPopupIFrame").addClass(popUpOptions.iframeClass);
    $axPopupModal.find("#axPopupTitle").html(popUpOptions.title);
    $axPopupModal.find("#axPopupIFrame").attr('src', 'Popup?load=' + popUpOptions.iframeUrl);
    $axPopupModal.modal('show');
}

function bindDcData(dataObj, arrDc) {
    arrDc.forEach(function (dcNo) {

        $('.ax-dc[data-ax-dcno=' + dcNo + ']:not(.ax-select2)').each(function () {
            let $dc = $(this);
            if ($dc.hasClass('ax-nongrid')) {
                let rowData = dataObj['DC' + dcNo.toString()][0] || dataObj['DC' + dcNo.toString()][1];
                bindDcRowData($dc, rowData);
            }
            else {
                $dc.find('.ax-dcrow').each(function () {
                    let $row = $(this);
                    if ($row.hasClass('ax-tempgridrow')) { //Multi row binding
                        try {
                            for (let row in dataObj['DC' + dcNo.toString()]) {
                                let rowData = dataObj['DC' + dcNo.toString()][row];
                                let skipRow = false;
                                let filter = $row.attr("data-ax-filter");
                                if (typeof filter != "undefined") {
                                    filter = filter.split('=');
                                    if (filter.length == 2 && rowData[filter[0]] != filter[1]) {
                                        skipRow = true;
                                    }
                                }

                                if (!skipRow) {
                                    copyHTML({
                                        sourceId: $row.attr('data-ax-copyhtmlid'),
                                        sourceHtml: $row.outerHTML(),
                                        targetElem: $row.closest('.ax-dc'),
                                        rowData: rowData
                                    })
                                }                                                               
                            }
                        }
                        catch (ex) {
                            console.log("JS Error: " + ex);
                        }
                    }
                    else { //Single row binding             
                        bindDcRowData($row, dataObj['DC' + dcNo.toString()][0] || dataObj['DC' + dcNo.toString()][1])
                    }

                });
            }
        });

        $('.ax-dc[data-ax-dcno=' + dcNo + '].ax-select2-grid').each(function () {
            bindSelect2Data($(this), dataObj['DC' + dcNo.toString()]);
        });
        $('.ax-dc[data-ax-dcno=' + dcNo + ']').find('.ax-select2-nongrid').addBack('.ax-select2-nongrid').each(function () {
            bindSelect2NonGridData($(this), dataObj['DC' + dcNo.toString()]);
        });

    });
}

function bindSelect2NonGridData($elem, rowDataList) {
    let textCol = $elem.attr("data-ax-select2-textcol");
    for (let row in rowDataList) {
        let rowData = rowDataList[row];
        if (typeof rowData[textCol] != "undefined") {
            var valArr = rowData[textCol].split(',');
            valArr.forEach(function (val) {
                if ($elem.find("option[value='" + val + "']").length == 0) {
                    $elem.append($("<option selected='selected'></option>").val(val).text(val));
                }
            });
            $elem.val(valArr).trigger('change');
        }
    }    
}

function bindSelect2Data($elem, rowDataList) {
    let valArr = [];
    let idCol = $elem.attr("data-ax-select2-idcol");
    let textCol = $elem.attr("data-ax-select2-textcol");
    for (let row in rowDataList) {
        let rowData = rowDataList[row];

        if ($elem.find("option[value='" + rowData[idCol] + "']").length > 0) {
            $elem.find("option[value='" + rowData[idCol] + "']").each(function () {
                $(this).attr('data-ax-dcrowid', rowData.dcrowid).attr('data-ax-dcrowno', rowData.dcrowno)
            });
            valArr.push(rowData[idCol]);
        } else {
            $elem.append(getSelectedOption({ id: rowData[idCol], text: rowData[textCol], dcrowid: rowData.dcrowid, dcrowno: rowData.dcrowno }));
            valArr.push(rowData[idCol]);
        }
    }
    $elem.val(valArr).trigger('change');
}

function bindDcRowData($row, rowData) {
    if (typeof rowData == "undefined")
        return;

    if (typeof rowData.dcrowid != "undefined")
        $row.attr('data-ax-dcrowid', rowData.dcrowid);

    if (typeof rowData.dcrowno != "undefined")
        $row.attr('data-ax-dcrowno', rowData.dcrowno);

    $row.find('.ax-savefld').addBack('.ax-savefld').each(function () {
        let $fld = $(this);
        let value = rowData[$fld.attr('data-ax-fld')];
        if (typeof value != "undefined" && value != "") {
            $fld.removeClass("ax-tempgridrow");
            setFieldValue({ field: $fld, value: value });
        }
    });

    try {
        $row.find('.ax-removehtml[data-target="#' + $row.attr('id') + '"]').attr('data-ax-dcrowid', rowData.dcrowid);
    }
    catch (ex) { }
}

function deleteDcRowFromDB(deleteOptions) {
    saveData({
        formId: deleteOptions.formId,
        saveJson: function () {
            let saveJson = {};
            saveJson.rowid = deleteOptions.rowid;
            return saveJson;
        }
    });
}

//#region Tstruct Save Data Generation
function getTstructSaveData(formId) {
    if (formId.indexOf("#") == -1) {
        formId = "#" + formId
    }

    let $form = $(formId);
    if ($form.length == 0) {
        return;
    }

    tstGridDataObj = {};
    tstNonGridDataObj = {};
    $form.find('.ax-dc:not(.ax-nongrid)').each(function () {
        let $dc = $(this);
        getGridDcData($dc);
    });

    var nonGridDcs = {};
    var nonGridDcRowids = {};
    var nonGridDcRownos = {};
    $form.find('.ax-dc.ax-nongrid').each(function () {
        let $dc = $(this);
        nonGridDcs[$dc.attr('data-ax-dcno').toString()] = true;
        nonGridDcRowids[$dc.attr('data-ax-dcno').toString()] = $dc.attr('data-ax-dcrowid').toString();
        nonGridDcRownos[$dc.attr('data-ax-dcno').toString()] = $dc.attr('data-ax-dcrowno').toString();
    });

    for (var dcNo in nonGridDcs) {
        $form.find('.ax-dc.ax-nongrid[data-ax-dcno="' + dcNo + '"].ax-savefld, .ax-dc.ax-nongrid[data-ax-dcno="' + dcNo + '"] .ax-savefld').each(function () {
            let dcJson = {};
            dcJson.rowno = nonGridDcRownos[dcNo] || 0;
            dcJson.text = nonGridDcRowids[dcNo] || 0;

            let $fld = $(this);
            let columns = {};
            if (typeof $fld.attr("data-ax-fld") != "undefined") {
                columns[$fld.attr("data-ax-fld")] = getFieldValue($fld);
            }
            dcJson.columns = columns;

            let dcRecId = 'axp_recid' + dcNo;

            if (typeof tstNonGridDataObj[dcRecId] == "undefined") {
                tstNonGridDataObj[dcRecId] = dcJson;
            }
            else {
                let currJsonColumns = tstNonGridDataObj[dcRecId].columns;
                tstNonGridDataObj[dcRecId].columns = Object.assign(currJsonColumns, dcJson.columns);
            }
        })
    }

    let tstRecData = [];
    for (var key of Object.keys(tstNonGridDataObj)) {
        let tempObj = {};
        var tempArr = [];
        tempArr.push(tstNonGridDataObj[key]);
        tempObj[key] = tempArr;
        tstRecData.push(tempObj)
    }

    for (var key of Object.keys(tstGridDataObj)) {
        let tempObj = {};
        tempObj[key] = tstGridDataObj[key];
        tstRecData.push(tempObj)
    }

    return tstRecData;
}

function getGridDcData($dc) {
    let dcArr = [];
    $dc.find('.ax-dcrow:not(.ax-tempgridrow)').each(function () {
        let $row = $(this);
        dcArr.push(getGridDcRowJson($row))
    });

    let dcNo = $dc.attr("data-ax-dcno");
    let dcRecId = 'axp_recid' + dcNo;

    if (typeof tstGridDataObj[dcRecId] == "undefined") {
        tstGridDataObj[dcRecId] = dcArr;
    }
    else {
        let currArr = tstGridDataObj[dcRecId];
        tstGridDataObj[dcRecId] = currArr.concat(dcArr);
    }
}

function getNonGridDcData($dc) {

    let dcJson = {};
    dcJson.rowno = $dc.attr("data-ax-dcrowno");
    dcJson.text = $dc.attr("data-ax-dcrowid") || 0;
    let columns = {};
    $dc.find('.ax-savefld').addBack('.ax-savefld').each(function () {
        let $fld = $(this);
        if (typeof $fld.attr("data-ax-fld") != "undefined") {
            columns[$fld.attr("data-ax-fld")] = getFieldValue($fld);
        }
    });
    dcJson.columns = columns;

    let dcNo = $dc.attr("data-ax-dcno");
    let dcRecId = 'axp_recid' + dcNo;

    if (typeof tstGridDataObj[dcRecId] == "undefined") {
        tstGridDataObj[dcRecId] = dcArr;
    }
    else {
        let currArr = tstGridDataObj[dcRecId];
        tstGridDataObj[dcRecId] = currArr.concat(dcArr);
    }
}

function getGridDcRowJson($row) {
    let rowJson = {};
    rowJson.rowno = $row.attr("data-ax-dcrowno");
    rowJson.text = $row.attr("data-ax-dcrowid") || 0;
    let columns = {};
    $row.find('.ax-savefld').each(function () {
        let $fld = $(this);
        if (typeof $fld.attr("data-ax-fld") != "undefined") {
            columns[$fld.attr("data-ax-fld")] = getFieldValue($fld);
        }
    });
    rowJson.columns = columns;

    return rowJson;
}

function getNonGridDcJson($row) {
    let rowJson = {};
    rowJson.rowno = $row.attr("data-ax-dcrowno");
    rowJson.text = $row.attr("data-ax-dcrowid") || 0;
    let columns = {};
    $row.find('.ax-savefld').addBack('.ax-savefld').each(function () {
        let $fld = $(this);
        if (typeof $fld.attr("data-ax-fld") != "undefined") {
            columns[$fld.attr("data-ax-fld")] = getFieldValue($fld);
        }
    });
    rowJson.columns = columns;

    return rowJson;
}
//#endregion

//#region Select2 Logics
function getSelectedOption(rowData) {
    return $("<option selected='selected'></option>").val(rowData.id).text(rowData.text).attr('data-ax-dcrowid', rowData.dcrowid).attr('data-ax-dcrowno', rowData.dcrowno);
}

function createSelect2(select2Options) {

    let $elem = select2Options.elem;

    try {
        select2Options = generateSelect2Options(select2Options);
    } catch (ex) { };

    //let currVal = getFieldValue($elem);

    if (select2Options.ajaxData) {
        $elem.select2({
            minimumInputLength: 2,
            tags: select2Options.tags || false,
            ajax: {
                delay: 300,
                type: "POST",
                url: "../api/GetFilteredData?" + select2Options.dataId,
                headers: {
                    "Authorization": localStorage.getItem('RCP_JwtToken')
                },
                cache: false,
                contentType: "application/json;charset=utf-8",
                data: function (term) {
                    let dataOptions = {};
                    dataOptions.dataObj = {};
                    dataOptions.dataObj.key = select2Options.dataId;
                    dataOptions.dataObj.refresh = select2Options.refresh;

                    dataOptions.dataObj.dataParams = new Object();

                    if (dataOptions.showLoader == true) {
                        console.log(dataOptions.dataId)
                        displayLoader("show");
                    }

                    if (typeof select2Options.beforeLoad != "undefined") {
                        dataOptions = select2Options.beforeLoad(dataOptions);
                    }

                    dataOptions.dataObj.dataFilter = term.term;

                    return JSON.stringify(dataOptions.dataObj)
                },
                async: true,
                dataType: "json",
                processResults: function (data, params) {
                    if (typeof select2Options.afterLoad != "undefined") {
                        data = select2Options.afterLoad(data);
                    }

                    return {
                        results: data
                    };
                }
            }
        });
        //if (currVal != "")
        //    $elem.select2('val', currVal).trigger('change');
    }
    else {
        $.when(loadDataAsync({
            dataId: select2Options.dataId,
            refresh: select2Options.refresh
        })).then(function (data, textStatus, jqXHR) {
            //let currVal = $elem.val();
            let tempData = dataConvert({ data: data, dataId: select2Options.dataId });
            $elem.select2({
                closeOnSelect: false,
                data: tempData,
                dropdownParent: select2Options.dropdownParent,
                tags: select2Options.tags || false
            });

            //if (currVal != "")
            //    $elem.select2('val', currVal).trigger('change');
        })
    }

    //let currVal = setFieldValue({ field: $elem, value: currVal });

    $elem.on('select2:unselecting', function (e) {
        let $select2Elem = $(this);
        $(this).data('unselecting', true);
        let $unselectedOption = $(e.params.args.data.element);
        let optionId = e.params.args.data.id;
        let dcRowId = $unselectedOption.attr('data-ax-dcrowid') || "0";
        if (dcRowId != "0") {
            let deleteAPI = $elem.attr('data-ax-dcrowdelete');
            if (typeof deleteAPI != "undefined") {
                confirmAlert({
                    title: "Confirm delete",
                    message: "Do you want to permanently delete the data?",
                    yesCaption: "Confirm",
                    noCaption: "No",
                    yesClick: function () {
                        deleteDcRowFromDB({ formId: deleteAPI, rowid: dcRowId });
                        let $option = $select2Elem.find('option[value="' + optionId + '"]');
                        $option.prop('selected', false);
                        $select2Elem.trigger('change.select2');
                    }
                })
            }
            e.preventDefault();
        }
    }).on('select2:opening', function (e) {
        if ($(this).data('unselecting')) {
            $(this).removeData('unselecting');
            e.preventDefault();
        }
    });

}
//#endregion