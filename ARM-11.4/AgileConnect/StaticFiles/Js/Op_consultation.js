var pageParams = new Object();
var initAssessment_loadDataObj;
var consultation_loadDataObj;
var consultationSaveType = 'onhold';
var existingComplaints = '';
var complaintsRowNo = 0;

$(document).ready(function () {
    $('.selectpicker').selectpicker();

    $(".filter-option-inner-inner").append("<span class='onoffct-i'><i class='fa fa-angle-down' aria-hidden='true'></i></span>");

    setPageParams();
    pageParams = Object.assign({}, window.parent.pageParams);

    $("#opnumber").val(pageParams.opno);
    $("#doc_no").val(pageParams.appno);
    $("#uhid").val(pageParams.uhid);
    $("#doctor").val(pageParams.doctorid);
    $("#doctor_id").val(pageParams.doctorid);
    $("#op_consultation_save").find(".ax-dc.ax-nongrid[data-ax-dcno='1'],.ax-dc.ax-nongrid[data-ax-dcno='5']").each(function () {
        $(this).attr("data-ax-dcrowid", pageParams.consid);
    });

    //$("#generalAdviseList").select2({
    //    closeOnSelect: true,
    //    placeholder: "Enter general advise here...",
    //    allowClear: true
    //});  

     $(".onhold").click(function () {
        if ($(this).hasClass('TC_EndVideoYes')) {
            doConsultationSave('hold', true);
        }
        else {
            doConsultationSave('hold', false);
        }
    })

    $("input[type=radio][name=yesnoTel]").unbind('click').click(function () {
        if ($(this).val() == 'telYes') {
            $("#telYesDiv").show();
            $("#telNoDiv").hide();
        }
        else {
            $("#telYesDiv").hide();
            $("#telNoDiv").show();
        }
    })    

    $("#oldMedicines").hide();

    loadAPIData();

    enableDisableEmrActions();
});


function enableDisableEmrActions() {
    return;
    var selectedDate = window.parent.pageParams.opdate;

    if (getDayDiff(selectedDate) < -1 && window.parent.pageParams.status == "Waiting") {
        //Previous day appointment missed start consultation -- Should allow all the activity that are kept for Start consultation or Teleconsultation for only one day
        $("#drconsultation .container").addClass("ax-disabled-modal");
        $("#drconsultation .add-new-complaints").addClass("ax-disabled");
        $(".modal-footer button").addClass("ax-disabled");
    }

    if (getDayDiff(selectedDate) < -7 && window.parent.pageParams.status == "Onhold") {
        //Previous day appointment - Onhold: Should allow all the activity that are kept only for On - Hold for Upto One week
        $("#drconsultation .container").addClass("ax-disabled-modal");
        $("#drconsultation .add-new-complaints").addClass("ax-disabled");
        $(".modal-footer button").addClass("ax-disabled");
    }
}

function getDayDiff(date) {
    if (moment(date).isSame(moment(), 'day')) {
        return 0
    }
    else if (moment().diff(date) < 0) {
        return 1
    }
    else
        return (-1 * moment().diff(date, 'days'));
}

function doConsultationSave(action, doVisitEntry) {
    doInitAssessmentSave();
    if (action == 'hold') {
        if (doVisitEntry && pageParams.opno.indexOf('OP') == -1) {
            consultationSaveType = 'onhold'
            $('.visit-entry').click();
        }
        else {
            consultationSaveType = 'onhold'
            $('.cons-hold').click();
        }
    }
    else if (action == 'end') {
        if (pageParams.opno.indexOf('OP') == -1) {
            consultationSaveType = 'end';
            $('.visit-entry').click();
        }
        else {
            consultationSaveType = 'end';
            $('.cons-end').click();
        }
    }
}

async function loadAPIData() {    
    loadData({
        dataId: 'op_consultationData',
        beforeLoad: function (dataOptions) {
            dataOptions.dataObj.dataParams.recid = pageParams.consid;
            return dataOptions;
        },
        afterLoad: function (data, dataOptions) {
            consultation_loadDataObj = generateLoadTstructDataObject(data);
            tstGridDcRowNos['DC3'] = 0;
            tstGridDcRowNos['DC4'] = 0;
            bindDcData(consultation_loadDataObj, ['1', '2', '3', '4', '5', '7']);

            if (typeof consultation_loadDataObj.DC3 != "undefined" && typeof consultation_loadDataObj.DC3["1"] == "undefined") {
                loadData({
                    dataId: 'op_consultation_prevmeds',
                    beforeLoad: function (dataOptions) {
                        dataOptions.dataObj.dataParams.uhid = pageParams.uhid;
                        dataOptions.dataObj.dataParams.userid = localStorage["AxpertConnectUser"];
                        return dataOptions;
                    },
                    afterLoad: function (data, dataOptions) {
                        if (data == "" || data.length == 0) {
                            removeOldMedications();
                            $("#oldMedicines").hide();
                            $("#oldMedicines .pres-icon").hide();                            
                        }
                        else {
                            $("#oldMedicines").show();
                            $("#newMedicine thead").addClass("invisible");
                            let $row = $("#oldMedRow_ax-rownum");
                            $row.closest('tbody').addClass('ax-dc').attr('data-ax-dcno', '3');
                            for (let dataRow in data) {
                                copyHTML({
                                    sourceId: 'medRow',
                                    sourceHtml: $row.outerHTML(),
                                    targetElem: $row.closest('.ax-dc'),
                                    rowData: data[dataRow]
                                })
                            }
                        }
                    }
                });
            }
            if ($("#completed").val() == '1') {
                $("#drconsultation .container").addClass("ax-disabled-modal");
                $("#drconsultation .add-new-complaints").addClass("ax-disabled");
                $(".modal-footer button").addClass("ax-disabled");
            }

            $.when(isAPIDataLoaded('op_consultation_dietadvise'))
                .then(function () {
                    bindDcData(consultation_loadDataObj, ['8']);
                })

            $.when(isAPIDataLoaded('op_consultation_reviewinvestigation'))
                .then(function () {
                    bindDcData(consultation_loadDataObj, ['9']);
                })

            let bp = $('#blood_pressure').val() || '';
            if (bp != '' && bp.split('/').length == 2) {
                $('#systolic').val(bp.split('/')[0]);
                $('#diastolic').val(bp.split('/')[1]);
            }

            if ($("#doctor-list").val().trim() != "") {
                $("#crossyes").prop('checked', 'checked');
            }

            if (pageParams.inititalassessmentid != "0") {
                loadData({
                    dataId: 'op_initialassessmentData',
                    beforeLoad: function (dataOptions) {
                        dataOptions.dataObj.dataParams.recid = pageParams.inititalassessmentid;
                        return dataOptions;
                    },
                    afterLoad: function (data, dataOptions) {
                        let temptstGridDcRowNos = JSON.parse(JSON.stringify(tstGridDcRowNos));
                        tstGridDcRowNos = {};
                        initAssessment_loadDataObj = generateLoadTstructDataObject(data);
                        tstGridDcRowNos = {};
                        tstGridDcRowNos = temptstGridDcRowNos;
                        $.when(isAPIDataLoaded('op_complaintsList'))
                            .then(function () {
                                //bindSelect2Data($("#presentComplaints"), initAssessment_loadDataObj['DC2']);
                                bindInitialAssessmentData(['DC2']);
                                existingComplaints = $("#presentComplaints").val().join(',');
                            })
                    }
                });
            }
        }
    });
}

function bindConsultationData(loadDCArr) {
    if (typeof consultation_loadDataObj == "undefined" || loadDCArr.length == 0)
        return;

    if (loadDCArr.indexOf("DC2") > -1) {
        let valArr = [];
        let $diagnosis = $('#diagnosisList');
        let idCol = $diagnosis.attr("data-ax-select2-idcol");
        let textCol = $diagnosis.attr("data-ax-select2-textcol");
        for (let row in consultation_loadDataObj.DC2) {
            let rowData = consultation_loadDataObj.DC2[row];

            if ($diagnosis.find("option[value='" + rowData[idCol] + "']").length > 0) {
                $diagnosis.find("option[value='" + rowData[idCol] + "']").each(function () {
                    $(this).attr('data-ax-dcrowid', rowData.dcrowid).attr('data-ax-dcrowno', rowData.dcrowno)
                });
                valArr.push(rowData[idCol]);
            } else {
                $diagnosis.append(getSelectedOption({ id: rowData[idCol], text: rowData[textCol], dcrowid: rowData.dcrowid, dcrowno: rowData.dcrowno }));
                valArr.push(rowData[idCol]);
            }
        }
        $diagnosis.val(valArr).trigger('change');
    }
}


function bindInitialAssessmentData(loadDCArr) {
    if (typeof initAssessment_loadDataObj == "undefined" || loadDCArr.length == 0)
        return;

    if (loadDCArr.indexOf("DC2") > -1) {
        let valArr = [];
        let $complaints = $('#presentComplaints');
        for (let row in initAssessment_loadDataObj.DC2) {
            let rowData = initAssessment_loadDataObj.DC2[row];
            if ((typeof rowData.opno_complaints != "undefined" && rowData.opno_complaints == pageParams.opno) || (typeof rowData.app_no_complaints != "undefined" && rowData.app_no_complaints == pageParams.appno)) {
                if ($complaints.find("option[value='" + rowData.complaintsid + "']").length > 0) {
                    $complaints.find("option[value='" + rowData.complaintsid + "']").each(function () {
                        $(this).attr('data-ax-dcrowid', rowData.dcrowid).attr('data-ax-dcrowno', rowData.dcrowno)
                    });
                    valArr.push(rowData.complaintsid);
                } else {
                    $complaints.append(getSelectedOption({ id: rowData.complaintsid, text: rowData.complaints, dcrowid: rowData.dcrowid, dcrowno: rowData.dcrowno }));
                    valArr.push(rowData.complaintsid);
                }
            }
        }
        $complaints.val(valArr).trigger('change');
    }
}


$(function () {
    $('#smartwizard').smartWizard({
        selected: 0,
        theme: 'dots',
        autoAdjustHeight: true,
        transitionEffect: 'fade',
        showStepURLhash: false,
        anchorSettings: {
            anchorClickable: true,
            enableAllAnchors: true,
        }
        ,
        keyboardSettings: {
           keyNavigation: false
        }
    }).on("leaveStep", function (e, anchorObject, stepNumber, stepDirection) {
        try {
            return validateStep(stepNumber);
        }
        catch (ex) { };
    
        return true;
    });

    if (pageParams.status == "Completed") {
        $("#drconsultation .container").addClass("ax-disabled-modal");
        $("#drconsultation .add-new-complaints").addClass("ax-disabled");
        $(".modal-footer button").addClass("ax-disabled");
    }
    
    $('.patient-onhold').on('click', function (e) {
        e.preventDefault();
        $('.modal-pop-onhold').toggleClass('is-visible');
    });
    $('.patient-review').on('click', function (e) {
        e.preventDefault();
        $('.modal-pop-review').toggleClass('is-visible');
    });

    $("#sentlab").hide();
    $("#tolab").click(function () {
        if ($(this).is(":checked")) {
            $("#sentlab").show();
            $("#notsentlab").hide();
        } else {
            $("#sentlab").hide();
            $("#notsentlab").show();
        }
    });

    $("#sentpharmacy").hide();
    $("#senttopharmacy").click(function () {
        if ($(this).is(":checked")) {
            $("#sentpharmacy").show();
            $("#notsentpharmacy").hide();
        } else {
            $("#sentpharmacy").hide();
            $("#notsentpharmacy").show();
        }
    });    

    $('.blood_pressure').on('change keyup', function () {
        $('#blood_pressure').val((($('#systolic').val() || '') + "/" + ($('#diastolic').val() || '')));
    })
});

function removeOldMedications() {
    $('#medicinetable tbody').html('');    
}

function setPageParams() {
    var params = $("#divHome").attr("data-params");
    if (typeof params != "undefined") {
        params = params.split('&');

        for (var i = 0; i < params.length; i++) {
            let tempParam = params[i].split('=');
            if (tempParam.length == 2)
                pageParams[tempParam[0]] = tempParam[1];
        }
    }
}

function generateCopyHtmlOptions(copyHtmlOptions) {
    if (copyHtmlOptions.sourceId == "medRow") {
        copyHtmlOptions.afterCopy = function(copyHtmlOptions){
            createMagicSearch({
                elem: $(copyHtmlOptions.newElem).find('td input.doseUnit'),
                dataSource: [{ doseunit: "Days" }, { doseunit: "Weeks" }, {doseunit: "Months"}],
                field: 'doseunit',
                dataId: 'op_doseUnit'
            });
    
            duplicateCheckEvent(".medical:not('#invesTable')");            
        }
    }
    else if (copyHtmlOptions.sourceId == "invesRow") {
        copyHtmlOptions.afterCopy = function(copyHtmlOptions){
            $(copyHtmlOptions.target).find('.investigationsList').change(function (e) {
                if ($(this).val() != "") {
                    $("#tolab").prop("checked", true);
                }
            });

            duplicateCheckEvent("#invesTable");
        }
    }
    return copyHtmlOptions;
}

$('#drconsultation').on('shown.bs.modal', function (e) {
    if (pageParams.status == "Completed") {
        $("#drconsultation .container").addClass("ax-disabled-modal");
        $("#drconsultation .add-new-complaints").addClass("ax-disabled");
        $(".modal-footer button").addClass("ax-disabled");
    }
}).on('hidden.bs.modal', function (e) {
    $("#drconsultation .container").removeClass("ax-disabled-modal");
    $("#drconsultation .add-new-complaints").addClass("ax-disabled");
    $(".modal-footer button").addClass("ax-disabled");
})



function startVideoConsultation() {
    loadData({
        dataId: 'op_getvideotoken'
    });
}

function onConsultationClose() {
    if ($("#drconsultation .container").hasClass("ax-disabled-modal")) {
        $('#consultationiframe').modal('hide');
        $('.modal-backdrop').remove();
    }
    else {
        confirmAlert({
            title: "Confirmation",
            message: "Do you want to close without saving the data?",
            yesCaption: "On Hold & Close",
            noCaption: "Close without saving",
            yesClick: function () {
                $(".cons-hold").click();
            },
            noClick: function () {
                $('#consultationiframe').modal('hide');
                $('.modal-backdrop').remove();
            }
        })
    }
}

function generateSaveDataOptions(saveDataOptions) {
    if (saveDataOptions.formId == "op_consultation_save") {

        saveDataOptions.saveJson = function (currJson) {
            var saveJson = {};

            
            var rowno = tstGridDcRowNos['DC2'] || 0;
            rowno = rowno + 1;

            var dc2JsonArr = new Array();            

            var dataArr = getFieldValue($("#diagnosisList"));
            for (var i = 0; i < dataArr.length; i++) {
                let $option = $(dataArr[i].element);
                var dc2Json = new Object();
                dc2Json.rowno = $option.attr('data-ax-dcrowno') || rowno;
                dc2Json.text = $option.attr('data-ax-dcrowid') || 0;
                dc2Json.columns = {
                    "problemlistcode": dataArr[i].text,
                    "icd": dataArr[i].id
                }
                dc2JsonArr.push(dc2Json);
                rowno++;
            }

            saveJson['axp_recid2'] = dc2JsonArr;                        

            rowno = tstGridDcRowNos['DC8'] || 0;
            rowno = rowno + 1;
            var dc8JsonArr = new Array();
            var dataArr = getFieldValue($("#dietAdviseList"));
            for (var i = 0; i < dataArr.length; i++) {
                let $option = $(dataArr[i].element);
                var dc8Json = new Object();
                dc8Json.rowno = $option.attr('data-ax-dcrowno') || rowno;
                dc8Json.text = $option.attr('data-ax-dcrowid') || 0;
                dc8Json.columns = {
                    "dietname": dataArr[i].text,
                    "dietaryitemsid": dataArr[i].id
                }
                dc8JsonArr.push(dc8Json);
                rowno++;
            }

            saveJson['axp_recid8'] = dc8JsonArr;

            rowno = tstGridDcRowNos['DC9'] || 0;
            rowno = rowno + 1;
            var dc9JsonArr = new Array();
            var dataArr = getFieldValue($("#reviewInvestigationList"));
            for (var i = 0; i < dataArr.length; i++) {
                let $option = $(dataArr[i].element);
                var dc9Json = new Object();
                dc9Json.rowno = $option.attr('data-ax-dcrowno') || rowno;
                dc9Json.text = $option.attr('data-ax-dcrowid') || 0;
                dc9Json.columns = {
                    "servicename_nxtvisit": dataArr[i].text,
                    "serviceid_nxtvisit": dataArr[i].id
                }
                dc9JsonArr.push(dc9Json);
                rowno++;
            }

            saveJson['axp_recid9'] = dc9JsonArr;

            for (var key in saveJson) {
                let tempObj = {};
                tempObj[key] = saveJson[key];
                currJson.recdata.push(tempObj);
            }
            return currJson;
        };
        saveDataOptions.afterSave = function (data) {
            $('.modal-pop-review').removeClass('is-visible');
            $('.modal-pop-onholdwalkin').removeClass('is-visible');
            $('.modal-pop-onholdtele').removeClass('is-visible');
            window.parent.$(".consultation-ref").click();
            try {
                if (data.result[0].message[0].msg.toString().indexOf("Prescription Entry EMR Saved") > -1 && consultationSaveType == 'end') {
                    $("#drconsultation .container").addClass("ax-disabled-modal");
                    $("#drconsultation .add-new-complaints").addClass("ax-disabled");
                    $(".modal-footer button").addClass("ax-disabled");
                    pageParams.status = "Completed";
                    window.parent.pageParams.status = "Completed";
                    window.parent.$(".load-send_doc, .load-upload_doc, .load-view_doc, #printDoc").removeClass("ax-disabled");
                }
            }
            catch (ex) { }

            messageAlert({
                title: "Confirmation",
                message: "Consultation is saved.",
                onclose: function () {
                    window.parent.$('#consultationiframe').modal('hide');
                    window.parent.$('.modal-backdrop').remove();
                }
            })
        };
        saveDataOptions.validateSave = function () {
            if (consultationSaveType == 'end') {
                if ($("input:radio[name='nextreview']:checked").length == 0) {
                    ToastMaker("Review Consultation is mandatory");
                    return false;
                }
                
                if ($("#reviewinvest").is(':checked')) {
                    if ($('#reviewInvestigationList').val() == '') {                        
                        ToastMaker("Please select review investigations");
                        return false;
                    }
                }

                if ($("input:radio[name='nextreview']:checked").val() == 'T') {
                    if ($('#next_period').val() == '') {
                        ToastMaker("Please select review period");
                        return false;
                    }

                    if ($('#next_duration').val() == '') {
                        ToastMaker("Please select review duration");
                        return false;
                    }

                    if ($('#reviewDate').val() == '') {
                        ToastMaker("Please select review date");
                        return false;
                    }
                }
            }
            return true;
        }
        saveDataOptions.beforeSave = function () {
            $("#opnumber").val(pageParams.opno);
            $("#recordid").val(pageParams.consid);

            if (consultationSaveType == 'end') {
                $("#completed").val("1");
            }
            else if (consultationSaveType == 'hold') {
                $("#completed").val("3");
            }
        }
    }
    else if (saveDataOptions.formId == "op_visitentry_save") {
        saveDataOptions.saveJson = function () {
            var saveJson = {};
            saveJson["company"] = pageParams.patientDetails.companyname;
            saveJson["branch"] = pageParams.patientDetails.branchname;
            saveJson["app_no"] = pageParams.patientDetails.appno;
            saveJson["source"] = pageParams.patientDetails.source;
            saveJson["uhid"] = pageParams.patientDetails.uhid;
            saveJson["mobile_primary"] = pageParams.patientDetails.mobileno;
            saveJson["attending_physician"] = pageParams.patientDetails.doctor_name;
            saveJson["patient_name"] = pageParams.patientDetails.patient_name;
            saveJson["payment_amount"] = pageParams.patientDetails.fee_amount;
            saveJson["age"] = pageParams.patientDetails.age;
            saveJson["agetype"] = pageParams.patientDetails.agetype;
            saveJson["sex"] = pageParams.patientDetails.sex;
            saveJson["salutation_patient_name"] = pageParams.patientDetails.salutation_patient_name;
            saveJson["consulting_fee"] = pageParams.patientDetails.fee_amount;

            return saveJson;
        };
        saveDataOptions.afterSave = function (data) {
            try {
                let opId = data.result[0].message[0].OP_No;
                pageParams.opno = opId;
                $("#opnumber").val(pageParams.opno);
                window.parent.pageParams.opno = opId;
                window.parent.pageParams.opId = opId;
                if (consultationSaveType == "end") {
                    doConsultationSave('end');
                }
                else {
                    doConsultationSave('hold',false);
                }
            }
            catch (ex) {
            }
        };
        saveDataOptions.beforeSave = function () {
            $("#recordid").val('0');
        }
    }
    else if (saveDataOptions.formId == "op_initial_assessment_save") {
        saveDataOptions.saveJson = function (currJson) {
            var saveJson = {};

            var dc1JsonArr = new Array();
            var dc1Json = new Object();
            dc1Json.rowno = "1";
            dc1Json.text = pageParams.inititalassessmentid;
            dc1Json.columns = {
                "uhid": pageParams.uhid,
                "doctorid": pageParams.doctorid
            }
            dc1JsonArr.push(dc1Json);
            saveJson['axp_recid1'] = dc1JsonArr;

            var rowno = parseInt(complaintsRowNo) || 0;
            rowno = rowno + 1;
            var dc2JsonArr = new Array();
            var dataArr = getFieldValue($("#presentComplaints"));
            for (var i = 0; i < dataArr.length; i++) {
                let $option = $(dataArr[i].element);
                var dc2Json = new Object();
                dc2Json.rowno = $option.attr('data-ax-dcrowno') || rowno;
                dc2Json.text = $option.attr('data-ax-dcrowid') || 0;
                dc2Json.columns = {
                    "complaints": dataArr[i].text,
                    "complaintsid": dataArr[i].id,
                    "opno_complaints": pageParams.opno,
                    "app_no_complaints": pageParams.appno
                }
                dc2JsonArr.push(dc2Json);
                rowno++;
            }

            saveJson['axp_recid2'] = dc2JsonArr;
            currJson.recdata = [];
            for (var key in saveJson) {
                let tempObj = {};
                tempObj[key] = saveJson[key];
                currJson.recdata.push(tempObj);
            }

            currJson.recdata.push(saveJson)
            return currJson;
        };
        saveDataOptions.beforeSave = function () {
            try {
                for (var item in initAssessment_loadDataObj.DC2) {
                    complaintsRowNo = initAssessment_loadDataObj.DC2[item].dcrowno;
                }
            }
            catch (ex) { }

            $("#recordid").val(pageParams.inititalassessmentid);
        };
        saveDataOptions.afterSave = function (data) {
            var recId = data.result[0].message[0].recordid;
            pageParams.inititalassessmentid = recId;
            window.parent.pageParams.inititalassessmentid = recId;

            if (window.parent.$("#initialassessmentctrl_ref").length > 0) {
                window.parent.$("#initialassessmentctrl_ref").click();
                window.parent.$("#consultation-ref").click();
            }
        };
    }
    else if (saveDataOptions.formId == "chiefcomplaint_save") {
        saveDataOptions.afterSave = function () {
            $("#addComplaint").val('');
            $('.modal-pop-newcomplaints').removeClass('is-visible');

            $('#presentComplaints').each(function () {
                let $elem = $(this);
                let dataId = $elem.attr('data-ax-datasource');
                if (typeof dataId != "undefined") {
                    let select2Options = { refresh: true, ajaxData: false, dataId: dataId, elem: $elem };
                    createSelect2(select2Options);
                }
            })
        };

        saveDataOptions.saveJson = function () {
            var saveJson = {};
            saveJson = generateSaveJSON('chiefcomplaint_save');
            return saveJson;
        }

    }
    return saveDataOptions;
}

function generateLoadDataOptions(dataOptions) {
    if (dataOptions.dataId == "op_medication_frequencyList") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.categoryname = dataOptions.categoryname;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };        
    }    
    else if (dataOptions.dataId == "op_consultation_diagnosisList") {
        //dataOptions.afterLoad = function (data) {
        //    pageParams.diagnosisSelection = [];
        //    $('#diagnosisList').magicsearch({
        //        dataSource: data,
        //        fields: ['dignosis'],
        //        id: 'dignosis',
        //        format: '%dignosis%',
        //        dropdownBtn: true,
        //        noResult: 'No Data',
        //        maxShow: 10,
        //        showSelected: true,
        //        multiple: true,
        //        isClear: false,
        //        success: function ($input, data) {
        //            if (pageParams.diagnosisSelection.indexOf(data.dignosis) == -1) {
        //                pageParams.diagnosisSelection.push(data.dignosis);
        //            }

        //            return true;
        //        },
        //        afterDelete: function ($input, data) {
        //            let index = pageParams.diagnosisSelection.indexOf(data.dignosis);
        //            if (index > -1) {
        //                pageParams.diagnosisSelection.splice(index, 1); 
        //            }
        //            return true;
        //        }

        //    });
        //}
    } 
    //else if (dataOptions.dataId == "op_consultation_dietadvise") {
    //    dataOptions.afterLoad = function () {
    //        $("#dietAdviseList").select2({
    //            //closeOnSelect: true,
    //            placeholder: "Enter Diet advise",
    //            //allowHtml: true,
    //            allowClear: true,
    //            //tags: true
    //        });
    //    }
    //} 
    //else if (dataOptions.dataId == "op_consultation_reviewinvestigation") {
    //    dataOptions.afterLoad = function () {
    //        $("#reviewInvestigationList").select2({
    //            closeOnSelect: true,
    //            placeholder: "Enter review investigation",
    //            allowHtml: true,
    //            allowClear: true,
    //            tags: true
    //        });
    //    }
    //} 
    //else if (dataOptions.dataId == "op_consultation_prevmeds") {
    //    dataOptions.afterLoad = function (data, dataOptions) {
    //        if (data == "" || data.length == 0) {                
    //            dataOptions.elem.html("");
    //            $("#oldMedicines").hide();
    //            $("#oldMedicines .pres-icon").hide();
    //            $("#newMedicine thead").removeClass("invisible");

    //        }
    //    };        
    //}    
    //else if (dataOptions.dataId == "op_complaintsList") {
    //    dataOptions.afterLoad = function (data) {
    //        $("#presentComplaints").select2({
    //            //closeOnSelect: true,
    //            placeholder: "Enter here...",
    //            //allowHtml: true,
    //            allowClear: true,
    //            //tags: true
    //        });

    //        loadData({
    //            dataId: 'op_consultation_presentcomplaints'
    //        })
    //    }
    //}
    else if (dataOptions.dataId == "op_consultation_presentcomplaints") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = pageParams.opno;
            dataParams.appno = pageParams.appno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            let valArr = [];
            for (var i = 0; i < data.length; i++) {
                valArr.push(data[i].present_complaints);
            }
            $("#presentComplaints").select2("val", valArr);

        };
    }
    return dataOptions;
}

function getKeyByValue(object, key, value) {
    return Object.keys(object).find(key => object[key] === value);
}


$(document).ready(function () {
    $('.ax-required:not(div):visible').off("blur").blur(function () {
        var elem = $(this);
        if (elem.val() == "") {
            var errmsg = elem.data("validation-msg");
            try {
                ToastMaker(errmsg);
                isValid = false;
                return false;
            } catch (ex) {
                alert(errmsg);
                isValid = false;
                return false;
            };
        }
    });

});

function doInitAssessmentSave() {
    if (existingComplaints != $("#presentComplaints").val().join(',')) {
        existingComplaints = $("#presentComplaints").val().join(',')
        saveData({
            formId: 'op_initial_assessment_save'
        });
    }

}

function validateStep(stepnumber) {
    if (stepnumber == 0) {        
        doInitAssessmentSave();
    }

    var isStepValid = true;
    if (stepnumber == 2) {
        if (checkDuplicate(".medical")) {
            isStepValid = false;
        }
        if (checkEmpty(".medical")) {
            isStepValid = false;
        }
    }
    if (stepnumber == 3) {
        if (checkDuplicate("#invesTable")) {
            isStepValid = false;
        }
        if (checkEmpty("#invesTable")) {
            isStepValid = false;
        }
    }

    return isStepValid;
}

function onholdwalktelecrtl() {
    sentmedicinetolab();
    senttomedicinepharmacy();
    if (pageParams.apptype == 'TC') {
        $('.modal-pop-onholdtele').toggleClass('is-visible');

    } else {
        $('.modal-pop-onholdwalkin').toggleClass('is-visible');
    }
}

function commonval() {
    var isValid = true;
    var classname = 'crossdrman';
    $('.' + classname + '').each(function (i, obj) {
        if (obj.value == '') {
            ToastMaker('Doctor Name Mandatory');
            isValid = false;
        }
    });
    var classname = 'surnameman';
    $('.' + classname + '').each(function (i, obj) {
        if (obj.value == '') {
            ToastMaker('Surgery Name Mandatory');
            isValid = false;
        }
    });
    if (isValid) {
        $('.modal-pop-review').toggleClass('is-visible');
        isValid = false;
    }
    return isValid;
}

function sentmedicinetolab() {
    if ($('.ordersetcheck:checked').length > 0) {
        $('#displayresultlabwalk').text('will be sent to Lab');
        $('.displayresultlabtele').text('will be sent to Lab');
    } else {
        $('#displayresultlabwalk').text('will not be sent to Lab');
        $('.displayresultlabtele').text('will not be sent to Lab');
    }
}

function senttomedicinepharmacy() {
    if ($('.senttopharmacy').is(":checked")) {
        $('#displayresultpharmacywalk').text('will be sent to pharmacy');
        $('.displayresultpharmacytele').text('will be sent to pharmacy');
    } else {
        $('#displayresultpharmacywalk').text('will not be sent to pharmacy');
        $('.displayresultpharmacytele').text('will not be sent to pharmacy');
    }
}

$(document).ready(function () {
    $('input#ifonlyreview').change(function () {
        if ($(this).is(":checked")) {

            $('input#next_period').addClass("ax-required");
            $('input#next_duration').addClass("ax-required");
            $('input#reviewDate').addClass("ax-required");
        } else {
            $('input#next_period').removeClass("ax-required");
            $('input#next_duration').removeClass("ax-required");
            $('input#reviewDate').removeClass("ax-required");
        }
    });
    $('input#noreview').change(function () {
        if ($(this).is(":checked")) {
            $('input#next_period').removeClass("ax-required");
            $('input#next_duration').removeClass("ax-required");
            $('input#reviewDate').removeClass("ax-required");
            $("input#next_period, input#next_duration, input#reviewDate").val("");
            $("input#reviewinvest").prop("checked", false);
            $('#reviewInvestigationList').val(null).trigger('change');
        }
    });

    $('#reviewinvest').change(function () {
        if ($(this).is(":checked")) {
            $('select.reviewinvestigationctrl').addClass("ax-required");
        } else {
            $('select.reviewinvestigationctrl').removeClass("ax-required");
        }
    });

    $('#reviewinvest').change(function () {
        if ($(this).is(":checked")) {
            $('select.reviewinvestigationctrl').addClass("ax-required");
        } else {
            $('select.reviewinvestigationctrl').removeClass("ax-required");
        }
    });

    $('#crossyes').change(function () {
        if ($(this).is(":checked")) {
            $('label.docname').addClass("control-label");
            $('input.crossdr').addClass("crossdrman");
        }
    });

    $('#crossno').change(function () {
        if ($(this).is(":checked")) {
            $('label.docname').removeClass("control-label");
            $('input.crossdr').removeClass("crossdrman");
        }
    });

    $('#yessur').change(function () {
        if ($(this).is(":checked")) {
            $('label.surnamectr').addClass("control-label");
            $('input.surnamectr').addClass("surnameman");
        }
    });
    $('#nosur').change(function () {
        if ($(this).is(":checked")) {
            $('label.surnamectr').removeClass("control-label");
            $('input.surnamectr').removeClass("surnameman");
        }
    });

    $('#next_period, #next_duration').change(function () {

        var getcount = $('#next_period').val();
        let getsel = $('#next_duration').val();
        getcount = parseInt(getcount);
        if (getcount != '' && getsel != '') {            
            let resultforday = moment(moment(), "DD/MM/YYYY").add(getcount, getsel.toLowerCase()).format('DD/MM/YYYY');

            $('#reviewDate').val(resultforday);

        }
    });

});

function generateSearchOptions(fldOptions) {
    let searchOptions = {};
    if (fldOptions.dataId == "op_medicineList") {
        searchOptions.onChange = function ($input, data) {
            if ($($input).val() != "") {
                $("#senttopharmacy").prop("checked", true);
            }

            $($input).closest('tr').find('td input.stockQty').val(data.stockqty);
            $($input).closest('tr').find('td input.med_id').val(data.itemid);

            loadData({
                dataId: 'op_medication_frequencyList',
                currElem: $input,
                categoryname: data.categoryname,
                afterLoad: function (data, dataOptions) {

                    if (data == '0' || data.length == 0) {
                        data = [{ 'frequency': 'NA' }]
                    }

                    createMagicSearch({
                        elem: $(dataOptions.currElem).closest('tr').find('td input.frequencyList'),
                        dataSource: data,
                        field: 'frequency',
                        dataId: dataOptions.dataId
                    });
                }
            })
        }
    }   
    else if (fldOptions.dataId == "op_medication_frequencyList" || fldOptions.dataId == "op_medication_durationList" || fldOptions.dataId == "op_doseUnit") {
        searchOptions.onChange = function ($input, data) {
            if (fldOptions.dataId == "op_medication_frequencyList") {
                $($input).closest('tr').find('td input.perdayqty').val(data.perday_qty);
                $($input).closest('tr').find('td input.freqid').val(data.cm_dosageid);
            }
            
            let perdayqty = $($input).closest('tr').find('td input.perdayqty').val();
            let duration = $($input).closest('tr').find('td input.durationList').val();
            let doseUnit = $($input).closest('tr').find('td input.doseUnit').val();
            let doseQty = 1;
            if (doseUnit == "Weeks") {
                doseQty = Math.round(perdayqty * duration * 7);
            }
            else if (doseUnit == "Months") {
                doseQty = Math.round(perdayqty * duration * 30);
            }
            else {
                doseQty = Math.round(perdayqty * duration);
            }
            if (isNaN(doseQty) || doseQty == "0") {
                $($input).closest('tr').find('td input.invoiceqty').val(duration);

                if (isNaN(duration) || duration == "0") {
                    $($input).closest('tr').find('td input.invoiceqty').val('1');
                }
            }
            else
                $($input).closest('tr').find('td input.invoiceqty').val(doseQty);

            if ($($input).closest('tr').find('td input.freqid').val() == '') {
                $($input).closest('tr').find('td input.freqid').val('0');
                $($input).closest('tr').find('td input.frequencyList').val('1')
            }
        }
    }
    else if (fldOptions.dataId == "op_consultation_orderInvestigations") {
        searchOptions.onChange = function ($input, data) {
            if ($($input).val() != "") {
                $("#tolab").prop("checked", true);
            }
            $($input).closest('tr').find('td input.serviceid').val(data.cm_serviceid);

        }
    }
    else if (fldOptions.dataId == "op_consultation_speciality") {
        searchOptions.onChange = function ($input, data) {
            $($input).closest('div.input-group').find('input.specialityid').val(data.specialityid);

            loadData({
                dataId: 'op_consultation_doctorname',
                currElem: $input,
                afterLoad: function (data, dataOptions) {                    

                    createMagicSearch({
                        elem: $(dataOptions.currElem).closest('div#step-5').find('input.crossdr'),
                        dataSource: data,
                        field: 'doctor_name',
                        dataId: dataOptions.dataId
                    });
                },
                beforeLoad: function (dataOptions) {
                    dataOptions.dataObj.dataParams = new Object();
                    dataOptions.dataObj.dataParams.speciality = $(".specialityid").val();
                    return dataOptions;
                }
            })
        }
    }
    else if (fldOptions.dataId == "op_consultation_doctorname") {
        searchOptions.onChange = function ($input, data) {
            $($input).closest('div.input-group').find('input.reftodoctorid').val(data.cm_doctorid);
        }
    }
    else if (fldOptions.dataId == "op_consultation_surgery") {
        searchOptions.onChange = function ($input, data) {
            $($input).closest('div.input-group').find('input.surgeryid').val(data.surgeryid);
        }
    }
    return searchOptions;
}

$(document).ready(function () {
    $('.ax-select2:not("#diagnosisList")').each(function () {
        let $elem = $(this);
        let dataId = $elem.attr('data-ax-datasource');

        let tags = $elem.attr('data-ax-tags');
        if (tags == "true") 
            tags = true;
        else
            tags = false;

        if (typeof dataId != "undefined") {
            let select2Options = { refresh: false, ajaxData: false, dataId: dataId, elem: $elem, tags: tags};
            createSelect2(select2Options);
        }
    })

    $('#diagnosisList').each(function () {
        let $elem = $(this);
        let dataId = $elem.attr('data-ax-datasource');
        if (typeof dataId != "undefined") {
            let select2Options = { refresh: false, ajaxData: true, dataId: dataId, elem: $elem };
            createSelect2(select2Options);
        }
    })

    $('.patient-onholdwalkincls').click(function () {
        sentmedicinetolab();
        senttomedicinepharmacy();
        $('.modal-pop-onholdwalkin').toggleClass('is-visible');
    })
    $('.modal-pop-onholdtelecls').click(function () {
        sentmedicinetolab();
        senttomedicinepharmacy();
        $('.modal-pop-onholdtele').toggleClass('is-visible');
    })

    $(".datepicker").datepicker({
        dateFormat: 'dd/mm/yy',
        orientation: 'auto',
        todayHighlight: true,
        autoclose: true,
        startDate: new Date(),
        minDate: 0
    });

    $(".reviewperiod").addClass('ax-disabled');
    $('#reviewInvestigationList').attr('disabled', 'disabled');

    $("input[name=nextreview]").off('change').change(function () {
        if ($(this).is(':checked') && $(this).val() == 'T') {
            $(".reviewperiod").removeClass('ax-disabled');
            $('#next_duration').val('Days')
        }
        else {
            $(".reviewperiod").addClass('ax-disabled');
            $(".reviewperiod").val('');
            $("#reviewinvest").prop('checked', false);
            $('#reviewInvestigationList').val('').trigger('change');
            $('#reviewInvestigationList').attr('disabled', 'disabled');
        }
    })

    $("#reviewinvest").off('change').change(function () {
        if ($(this).is(':checked')) {
            $('#reviewInvestigationList').removeAttr('disabled');
        }
        else {
            $('#reviewInvestigationList').val('').trigger('change');
            $('#reviewInvestigationList').attr('disabled', 'disabled');
        }
    })

    $('.add-new-complaints').on('click', function (e) {
        $("#addComplaint").val('');
        e.preventDefault();
        $('.modal-pop-newcomplaints').toggleClass('is-visible');
    });
});