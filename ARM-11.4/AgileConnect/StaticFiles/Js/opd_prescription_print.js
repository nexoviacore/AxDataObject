var consultation_loadDataObj;

$(document).ready(function () {

    loadData({
        dataId: 'op_consultationData',
        beforeLoad: function (dataOptions) {
            dataOptions.dataObj.dataParams.recid = window.parent.pageParams.consid;
            return dataOptions;
        },
        afterLoad: function (data, dataOptions) {
            consultation_loadDataObj = generateLoadTstructDataObject(data);
            bindDcData(consultation_loadDataObj, ['3', '5', '8', '9']);            
        }
    });

    apiCallsCount = 0;
    displayMainLoader("hide");
    displayLoader("hide");

    $.when(isAPIDataLoaded('op_consultationData'),isAPIDataLoaded('Doctorname_Regno_prescription'), isAPIDataLoaded('op_patientdashboard_header'), isAPIDataLoaded('op_patientdashboard_consultationdetails'), isAPIDataLoaded('op_patientdashboard_vitals'))
        .then(function () {
            apiCallsCount = 0;
            displayMainLoader("hide");
            displayLoader("hide");
            if ($("#captionGeneralAdvice").text().replaceAll(" ", "").replaceAll("\n", "") == "GeneralAdvice:") {
                $("#captionGeneralAdvice").hide();
            }
            if ($("#captionDietAdvice").text().replaceAll(" ", "").replaceAll("\n", "") == "DietAdvice:") {
                $("#captionDietAdvice").hide();
            }
            if ($("#captionNextReview").text().replaceAll(" ", "").replaceAll("\n", "") == "NextReview:") {
                $("#captionNextReview").hide();
            }
            if ($("#captionTests").text().replaceAll(" ", "").replaceAll("\n", "") == "Teststobetakenfornextvisit:") {
                $("#captionTests").hide();
            }
            printWithCustomFileName();
        })
})

function printWithCustomFileName() {
    var tempTitle = window.top.document.title;
    console.log(tempTitle);
    console.log(document);
    window.top.document.title = window.parent.pageParams.opno;
    window.print();
    window.top.document.title = tempTitle;
}

function generateLoadDataOptions(dataOptions) {
    if (dataOptions.dataId == "op_patientdashboard_header") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opdate = window.parent.pageParams.opdate;
            dataParams.doctorid = window.parent.pageParams.doctorid;
            dataParams.uhid = window.parent.pageParams.uhid;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };

    } else if (dataOptions.dataId == "op_patientdashboard_consultationdetails") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = window.parent.pageParams.opno;
            dataParams.appno = window.parent.pageParams.appno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
    } else if (dataOptions.dataId == "Doctorname_Regno_prescription" ) {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = window.parent.pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            try {
                $("#docName").html(data[0].doctor_name);
                $("#docDesignation").html(data[0].designation);
            }
            catch (e) { }
        };
    } else if (dataOptions.dataId == "Prescription_Branch_Address") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = window.parent.pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };        
    } else if (dataOptions.dataId == "op_patientdashboard_vitals") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.uhid = window.parent.pageParams.uhid;
            dataParams.opno = window.parent.pageParams.opno;
            dataParams.appno = window.parent.pageParams.appno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if (data == "" || data.length == 0) {
                dataOptions.elem.find('.card-body .comscr').html("No Vitals details available");
                dataOptions.elem.find('.card-header span:contains("{{")').html("");
                $("[data-source='op_patientdashboard_vitals']").html("");
            }
        };
    } else if (dataOptions.dataId == "op_patientdashboard_nextreview") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = window.parent.pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if (data == "" || data.length == 0) {
                dataOptions.elem.html("No Next Review details available");
            }
        }
    } else if (dataOptions.dataId == "op_patientdashboard_medication") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = window.parent.pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if (data == "" || data.length == 0) {
                dataOptions.elem.html("No Medication details available");
            }
        }
    }
    return dataOptions;
}
