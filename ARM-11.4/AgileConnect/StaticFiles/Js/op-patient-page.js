var pageParams = new Object();
$(document).ready(function () {
    $('.selectpicker').selectpicker();
    $('#onlineStatus').on('change', function (e) {
        $("#setDocStatus").click();
    });

    $(".filter-option-inner-inner").append("<span class='onoffct-i'><i class='fa fa-angle-down' aria-hidden='true'></i></span>");

    setPageParams();

    if (pageParams.opno.indexOf('OP') > -1) {
        $('.callPatient').addClass('ax-disabled');
    }

    $(".js-select2:not(#presentComplaints,#diagnosisList,#dietAdviseList,#reviewInvestigationList)").select2({
        closeOnSelect: true,
        placeholder: "Enter here...",
        allowHtml: true,
        allowClear: true,
        tags: true
    });       
   
    $(".callPatient").off('click').click(function () {
        startVideoConsultation();
    });

    if (pageParams.consid == "0") {
        $(".load-consultation").addClass('ax-hidden');
        if (pageParams.apptype == "TC") {
            $("#startConsult").addClass("disabled");
        }
        else {
            $("#teleConsult").addClass("disabled");
        }
    }
    else {
        $("#startConsult,#teleConsult").addClass("disabled");
    }

    $(".load-send_doc, .load-upload_doc, #printDoc").addClass("ax-disabled");
    if (pageParams.status == "Completed") {
        $(".load-send_doc, .load-upload_doc, #printDoc").removeClass("ax-disabled");
    }
       
});

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

// function beforeLoadData(dataObj, dataId, elem) {
//     {
//         dataObj.dataParams = new Object();
//         dataObj.dataParams.uhid = pageParams.uhid;
//         dataObj.dataParams.opno = pageParams.opno;
//         dataObj.dataParams.apmt_date = pageParams.apmt_date;
//         dataObj.dataParams.doctorid = pageParams.doctorid;
//         dataObj.dataParams.opdate = pageParams.opdate;
//         dataObj.dataParams.userid = localStorage["AxpertConnectUser"];
//         dataObj.dataParams.op_no = pageParams.opno;
//         dataObj.dataParams.app_no = pageParams.appno;
//     }
//     return dataObj;
// }

Date.prototype.timeNow = function () {
    return ((this.getHours() < 10) ? "0" : "") + ((this.getHours() > 12) ? (this.getHours() - 12) : this.getHours()) + ":" + ((this.getMinutes() < 10) ? "0" : "") + this.getMinutes() + ":" + ((this.getSeconds() < 10) ? "0" : "") + this.getSeconds() + ((this.getHours() > 12) ? (' PM') : ' AM');
};

//$('#drconsultation').on('shown.bs.modal', function (e) {
//    if (pageParams.status == "Completed") {
//        $("#drconsultation .step-content").addClass("ax-disabled-modal");
//    }
//}).on('hidden.bs.modal', function (e) {
//    $("#drconsultation .step-content").removeClass("ax-disabled-modal");
//})

function startVideoConsultation() {
    loadData({
        dataId: 'op_getvideotoken'
    });
}

function teleconsultationInvite() {
    loadData({
        dataId: 'op_getvideotoken2'
    });
}

function onConsultationClose() {
    if (pageParams.status == "Completed") {
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
                document.getElementById("consultation_load").contentWindow.document.querySelector("button[id='cons-hold2']").click();
            },
            noClick: function () {
                $('#consultationiframe').modal('hide');
                $('.modal-backdrop').remove();
            }
        })
    }
}

function generateSaveDataOptions(saveDataOptions) {
    if (saveDataOptions.formId == "op_consultation_start") {
        saveDataOptions.saveJson = function () {
            var saveJson = {};
            saveJson.recId = pageParams.consid;
            var dc1Json = new Object();
            dc1Json.rowno = 0;
            dc1Json.text = 0;

            dc1Json.columns = {
                "prescription_date": new Date().toDateFormat('dd-mmm-yyyy'), //JS Date Calc
                "uhid": pageParams.uhid,
                "opnumber": pageParams.opno,
                "doc_no": pageParams.appno,
                "doctor": pageParams.doctorid,
                "completed": "3",
                "active": "T"
            }           

            saveJson.dc1Json = dc1Json;            

            return saveJson;
        };
        saveDataOptions.afterSave = function (data) {
            try {
                var recId = data.result[0].message[0].recordid;
                pageParams.consid = recId;

                $("#startConsult,#teleConsult").addClass("disabled");
                $(".load-consultation").removeClass('ax-hidden');
                $('.load-consultation').click();
            }
            catch (ex) {                
            }
        };
    }    
    else if (saveDataOptions.formId == "op_set_doctorstatus") {
        saveDataOptions.saveJson = function () {
            var saveJson = {};
            saveJson.doctorstatus = $("#onlineStatus").find("option:selected").val().toString();
            saveJson.userid = localStorage["AxpertConnectUser"];
            return saveJson;
        }
    }
    else if (saveDataOptions.formId == "op_teleconsultation_invite") {
        saveDataOptions.saveJson = function () {
            var saveJson = {};
            if ($('.invite-docctrl:visible').length > 0) {
                saveJson.mobileno = $("#docmobile").val() ;
                saveJson.sendto = $("#docname").val() ;
            }
            else {
                saveJson.mobileno = $("#patmobile").val();
                saveJson.sendto = $("#patname").val();
            }
            
            saveJson.invitefrom = pageParams.doctorName;
            saveJson.invitelink = $("#inviteurl").val().toString();
            return saveJson;
        }
        saveDataOptions.afterSave = function () {
            messageAlert({
                title: "Confirmation",
                message: "Invitation sent."
            })
        }
    }
    return saveDataOptions;
}

function generateLoadDataOptions(dataOptions) {
    if (dataOptions.dataId == "op_patientdashboard_header") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opdate = pageParams.opdate;
            dataParams.doctorid = pageParams.doctorid;
            dataParams.uhid = pageParams.uhid;
            dataParams.appno = pageParams.appno;
            dataParams.opno = pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            pageParams.patientDetails = {};
            try {
                pageParams.patientDetails = data[0];
                pageParams.patientDetails['doctorid'] = pageParams.doctorid;
                pageParams.patientDetails['opno'] = pageParams.patientDetails.op_number;
                pageParams.patientDetails['appno'] = pageParams.appno;

                $(".pname-initial .material-icons").html(pageParams.patientDetails.patient_name.toUpperCase().substr(0, 1));
                $(".patient_name").html('Patient (' + pageParams.patientDetails.patient_name +') is')
            }
            catch (ex) {
            }
        }
    }
    else if (dataOptions.dataId == "op_username") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.userid = localStorage["AxpertConnectUser"];
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if (data[0].doctorid.toString() == "0") {
                $("#onlineStatus").prop("disabled", true);
                $('.selectpicker').selectpicker('refresh');
            }
            else {
                pageParams.doctorid = data[0].doctorid.toString();
                pageParams.doctorName = data[0].user_name.toString();
                loadData({
                    dataId: 'op_get_doctorstatus'
                });
            }
        }
    }
    else if (dataOptions.dataId == "op_patientdashboard_initialassessment") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.uhid = pageParams.uhid;
            dataParams.doctorid = pageParams.doctorid;
            dataParams.appno = pageParams.appno;
            dataParams.opno = pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
    }
    else if (dataOptions.dataId == "op_patientdashboard_consultationdetails") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = pageParams.opno;
            dataParams.appno = pageParams.appno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                dataOptions.elem.html("No Consultation details available");
            }            
        }
    }
    else if (dataOptions.dataId == "op_patientdashboard_pastvisithistory") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.uhid = pageParams.uhid;
            dataParams.opno = pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                dataOptions.elem.html("No Past Visit History details available");
            }            
        }
    }
    else if (dataOptions.dataId == "op_patientdashboard_vitals") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.uhid = pageParams.uhid;
            dataParams.opno = pageParams.opno;
            dataParams.appno = pageParams.appno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                dataOptions.elem.find('.card-body .comscr').html("No Vitals details available");
                dataOptions.elem.find('.card-header span:contains("{{")').html("");
            } 
            $('.load-vitals').off('click').on('click', function () {

                let paramStr = "inititalassessmentid=" + pageParams.inititalassessmentid + "&doctorid=" + pageParams.doctorid + "&uhid=" + pageParams.uhid + "&appno=" + pageParams.appno + "&opno=" + pageParams.opno + "&status=" + pageParams.status;
                pageParams.vitalsParams = new Object();
                pageParams.vitalsParams = getParamsObj(paramStr);

                $('#vitals-capture').modal('show');
                $('#vitals_load').attr('src', 'Popup?load=op_vitals');
            })
        };
    }
    else if (dataOptions.dataId == "op_patientdashboard_labresults") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.uhid = pageParams.uhid;
            dataParams.opno = pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                dataOptions.elem.html("No Lab Results available");
            }            
        }
    }
    else if (dataOptions.dataId == "op_patientdashboard_radiologyresults") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                dataOptions.elem.html("No Radiology Results available");
            }            
        }
    }
    else if (dataOptions.dataId == "op_patientdashboard_nextreview") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                dataOptions.elem.html("No Next Review details available");
            }            
        }
    }
    else if (dataOptions.dataId == "op_patientdashboard_medication") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                dataOptions.elem.html("No Medication details available");
            }            
        }
    }
    else if (dataOptions.dataId == "op_patientdashboard_opinion") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opno = pageParams.opno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                dataOptions.elem.html("No Opinion / Cross Consultation details available");
            }            
        }
    }
    else if (dataOptions.dataId == "op_get_doctorstatus") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.userid = localStorage["AxpertConnectUser"];
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if (data[0].doctor_status == "T") {
                $("select#onlineStatus").val("T");
            }
            else {
                $("select#onlineStatus").val("F");
            }

            $('.selectpicker').selectpicker('refresh');
        }
    } 
    else if (dataOptions.dataId == "op_getvideotoken") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataOptions.dataObj.refresh = true;
            dataParams = new Object();
            dataParams.appDate = moment(new Date()).format("YYYY-MM-DD");
            dataParams.appTime = moment(new Date()).format('HH:mm');
            dataParams.expiryTime = "1000";
            dataParams.roomName = pageParams.appno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            loadData({
                dataId: 'op_getvideo_shorturl',
                token: dataOptions.data.token
            });
        };
        
    }
    else if (dataOptions.dataId == "op_getvideo_shorturl") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataOptions.dataObj.refresh = true;
            dataParams.token = dataOptions.token;
            dataParams.appno = pageParams.appno;
            dataParams.doctorname = pageParams.doctorName;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            $("#videoBox").attr("src", dataOptions.data.shorturl);
            $('div.tele-innerctrlvideo').hide();
            $("#videoBox").show();
        };

    }
    else if (dataOptions.dataId == "op_getvideotoken2") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataOptions.dataObj.refresh = true;
            dataParams = new Object();
            dataParams.appDate = moment(new Date()).format("YYYY-MM-DD");
            dataParams.appTime = moment(new Date()).format('HH:mm');
            dataParams.expiryTime = "1000";
            dataParams.roomName = pageParams.appno;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            loadData({
                dataId: 'op_getvideo_shorturl2',
                token: dataOptions.data.token
            });
        };

    }
    else if (dataOptions.dataId == "op_getvideo_shorturl2") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataOptions.dataObj.refresh = true;
            dataParams.token = dataOptions.token;
            dataParams.appno = pageParams.appno;
            dataParams.doctorname = pageParams.doctorName;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            $('#inviteurl').val(dataOptions.data.shorturl);
            saveData({
                formId: 'op_teleconsultation_invite',
                url: dataOptions.data.shorturl
            });
        };

    }
    return dataOptions;
}

$(document).ready(function () {
    $('#onlineStatus').selectpicker();
    $(".filter-option-inner-inner").append(
        "<span class='onoffct-i'><i class='fa fa-angle-down' aria-hidden='true'></i></span>");
    $('.input-group .input-group-text i').remove();

    $("#emailid").hide();
    $("input[type='radio']").change(function () {
        if ($(this).val() == "whatsapp") {
            $("#emailid").hide();
            $("#whatsappnum").show();
        } else if ($(this).val() == "email") {
            $("#whatsappnum").hide();
            $("#emailid").show();
        }
    });

    $(".invitedoc-click").click(function () {
        $(".invite-patientctrl").hide();
        $(".invite-docctrl").show();
    });
    $(".invitepatient-click").click(function () {
        $(".invite-docctrl").hide();
        $(".invite-patientctrl").show();
    });

    $('.load-iniassment').on('click', function () {
        let paramStr = "inititalassessmentid=" + pageParams
            .inititalassessmentid + "&doctorid=" + pageParams.doctorid + "&uhid=" +
            pageParams.uhid + "&appno=" + pageParams.appno + "&opno=" + pageParams.opno + "&status=" + pageParams.status;
        pageParams.initAssessmentParams = new Object();
        pageParams.initAssessmentParams = getParamsObj(paramStr);

        $('#initialAssessmentModal').modal('show');
        $('#initial-ass').attr('src', 'Popup?load=Op_initialassessment');
    })


    $('.load-consultation').off('click').on('click', function () {
        $('#consultationiframe').modal('show');
        $('#consultation_load').attr('src', 'Popup?load=Op_consultation');
    })    

    $('.load-send_doc').off('click').on('click', function () {
        $('#sendDocument').modal('show');
        $('#send_doc_pop').attr('src', 'Popup?load=op_send_doc');
    })
    $('.load-upload_doc').off('click').on('click', function () {
        $('#uploadDoc').modal('show');
        $('#upload_doc_pop').attr('src', 'Popup?load=op_upload_doc');
    })
    $('.load-view_doc').off('click').on('click', function () {
        $('#viewDoc').modal('show');
        $('#view_doc_pop').attr('src', 'Popup?load=op_view_doc');
    })
    $('.load-visit_history').off('click').on('click', function () {
        $('#visitHistoryModal').modal('show');
        $('#visit_history_pop').attr('src', 'Popup?load=op_visit_history');
    })

    if (pageParams.load == "Consultation" || pageParams.load == "TeleConsultation") {
        if (pageParams.consid == "0") {
            if (pageParams.apptype == "TC") {
                $("#teleConsult").click();
            }
            else {
                $("#startConsult").click();
            }
        }
        else {
            $(".load-consultation").click();
        }
    }
});

function opdprescription() {
    showbg();
    var div = document.getElementById("opd_print_prescription");
    div.innerHTML = '<iframe src="Popup?load=opd_prescription_print" ></iframe>';
    const myTimeout = setTimeout(hidebg, 2000);
}

function showbg() {
    var loader = document.getElementById("clickloader");
    loader.style.display = 'block';
}
function hidebg() {
    var loader = document.getElementById("clickloader");
    loader.style.display = 'none';
}