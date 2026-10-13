var initalAssessmentHTML = "";
var clickedAllergyElem;
var pageParams = new Object();
pageParams.initAssessmentParams = new Object();

$(document).ready(function () {    
    $('.selectpicker').selectpicker();
    $('#onlineStatus').on('change', function (e) {        
        $("#setDocStatus").click();
    });
    
    $("#my-datepicker").text(moment().format("DD MMM"));
    $("#my-datepicker").attr("data-val", moment().format("DD-MMM-YYYY"));

    $('.input-group.date').datepicker({
        format: 'dd M',
        orientation: 'auto',
        todayHighlight: true,
        autoclose: true,
    }).on('changeDate', (e) => {
        //console.log(e.format(0, "dd MM"));
        if (e.format(0, "dd M") != "") {
            $("#my-datepicker").text(e.format(0, "dd M"));
            $("#my-datepicker").attr("data-val", e.format(0, "dd-M-yyyy"));
        }
        $(".loadCalendarData").click();
    });

    $('#sandbox-container').datepicker({
        startDate: new Date(),
        setDate: new Date(),
        format: 'dd M',
        orientation: 'auto',
        todayHighlight: true,
        autoclose: true,
    }).on('changeDate', (e) => {
        //console.log(e.format(0, "dd MM"));
        console.log($('#sandbox-container').datepicker('getDate').toDateFormat('dd-mmm-yyyy'));
        $("#rescheduleDate").val($('#sandbox-container').datepicker('getDate').toDateFormat('dd-mmm-yyyy'));
        $("#rescheduleDateChange").click();
    });
    
    $('#rescheduleAppointment').on('shown.bs.modal', function (e) {
 
        $("#rescheduleRecID").val($(e.relatedTarget).data('opappointmentid'));
        $("#rescheduleAppType").val($(e.relatedTarget).data('apptype'));
        pageParams.rescheduleDoctorID = $(e.relatedTarget).data('doctorid');
        $('#rescheduleAppointmentBtn').data('patient', $(e.relatedTarget).data('patient'));
        $('#rescheduleAppointmentBtn').data('appointmenttime', $(e.relatedTarget).data('appointmenttime'));
        $('#sandbox-container').datepicker("setDate", new Date());
        $("#rescheduleDate").val($('#sandbox-container').datepicker('getDate').toDateFormat('dd-mmm-yyyy'));
        $("#rescheduleTime").val($("input:radio[name='slot']:checked").val());

        loadData({
            dataId: 'op_reschedule_slots',
            showLoader: false
        });       
    }).on('hide.bs.modal', function (e) {
        $('#rescheduleRemarks').html("");
    })

    $('#cancelAppointment').on('shown.bs.modal', function (e) {
        $("#cancelRecID").val($(e.relatedTarget).data('opappointmentid'));
        $('#cancelPatientName').html($(e.relatedTarget).data('patient'));
        $('#cancelAppTime').html($("#my-datepicker").attr("data-val") + ", " + $(e.relatedTarget).data('appointmenttime'));
        $('#cancelAppType').html(getAppointmentType($(e.relatedTarget).data('appointmenttype')));
        $('#cancelRemarks').html("");
    }).on('hide.bs.modal', function (e) {
        $('#cancelPatientName').html("");
        $('#cancelAppTime').html("");
        $('#cancelAppType').html("");
    })

    $('#rescheduleAppointmentConfirmation').on('hide.bs.modal', function (e) {
        $('#rescheduleAppointmentConfirmationMsg').html("");
        $(".loadCalendarData-dbdata").click();
    })
    
    $('#search-button').on('click', function (e) {
        if ($('#search-input-container').hasClass('d-lg-none')) {
            e.preventDefault();
            $('#search-input-container').css('transform', 'scaleY(1)').removeClass('d-lg-none')
            $("#search").focus();
            return false;
        }
    });

    $('#hide-search-input-container').on('click', function (e) {
        e.preventDefault();
        $('#search-input-container').addClass('d-lg-none')
        return false;
    });
 
    setPageParams();

    $("#search").on("change keyup search", function () {
        let searchTxt = $(this).val().toLowerCase().trim();
        if (searchTxt == "") {
            $('tr.trPatient').removeClass('ax-hidden');
        }
        else {
            $('.collapse.trPatientDetails').collapse('hide');
            $('tr.trPatient').addClass('ax-hidden');
            $(".searchCol").each(function () {
                if ($(this).text().toLowerCase().indexOf(searchTxt) > -1) {
                    $(this).closest('tr.trPatient').removeClass('ax-hidden');
                }
            })
        }
    });
});

function getAppointmentType(typeId) {
    switch (typeId) {
        case "TC":
            return "Teleconsulatation"
            break;
        case "WI":
            return "Walkin"
            break;
        case "AP":
            return "Appointment"
            break;
    }
}

function generateLoadDataOptions(dataOptions) {
    if (dataOptions.dataId == "OP_Counts") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opdate = $("#my-datepicker").attr("data-val");
            dataParams.userid = localStorage["AxpertConnectUser"];
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            $('input[type=radio][name=op-calender-dashboard]').on('change', function () {
                var tableBody = $("#opPatientListTable tbody");
                tableBody.attr("data-tablefilter", $(this).attr("id"));
                $('.collapse.trPatientDetails').collapse('hide');
            });            
        };
    }    
    else if (dataOptions.dataId == "OP_PatientListData") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opdate = $("#my-datepicker").attr("data-val");
            dataParams.userid = localStorage["AxpertConnectUser"];
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            let $elem = dataOptions.elem;
            if(data == "" || data.length == 0){
                $elem.html("No patients available");
            }
            else{
                loadData({
                    dataId: 'op_patientlist_div1'
                });
                loadData({
                    dataId: 'op_patientlist_div2'
                });
                loadData({
                    dataId: 'op_patientlist_div3'
                });
    
                $elem.find("div.mobileStatus.TC").each(function () {
                    loadData({
                        dataId: 'op_user_mobiledetails',
                        elem: $(this),
                        refresh: false,
                    });
                });

                $.when(isAPIDataLoaded('op_username'))
                    .then(function () {
                        loadData({
                            dataId: 'op_patientlist_reviewdelay'
                        });
                    })

                bindEvents({events:['.ax-openinline', '.ax-openpopup']});
    
                $elem.find('.load-iniassment').off('click').on('click', function () {
                    let paramStr = $(this).attr('data-target-params');
                    pageParams.initAssessmentParams = new Object();
                    pageParams.initAssessmentParams = getParamsObj(paramStr);
                    $('#initialAssessmentModal').modal('show');
                    //$('#initial-ass').attr('src', 'Popup?load=Op_initialassessment&' + paramStr);
                    $('#initial-ass').attr('src', 'Popup?load=Op_initialassessment');
                })
    
                $elem.find('.load-send_doc').off('click').on('click', function () {
                    let paramStr = $(this).attr('data-target-params');
                    pageParams.docParams = new Object();
                    pageParams.docParams = getParamsObj(paramStr);
    
                    $('#sendDocument').modal('show');
                    $('#send_doc_pop').attr('src', 'Popup?load=op_send_doc');
                })
                $elem.find('.load-upload_doc').off('click').on('click', function () {
                    let paramStr = $(this).attr('data-target-params');
                    pageParams.docParams = new Object();
                    pageParams.docParams = getParamsObj(paramStr);
    
                    $('#uploadDoc').modal('show');
                    $('#upload_doc_pop').attr('src', 'Popup?load=op_upload_doc');
                })
                $elem.find('.load-view_doc').off('click').on('click', function () {
                    let paramStr = $(this).attr('data-target-params');
                    pageParams.docParams = new Object();
                    pageParams.docParams = getParamsObj(paramStr);
    
                    $('#viewDoc').modal('show');
                    $('#view_doc_pop').attr('src', 'Popup?load=op_view_doc');
                })
            }

            enableDisableEmrActions();
        };
    }
    else if (dataOptions.dataId == "op_reschedule_slots") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opdate = $('#sandbox-container').datepicker('getDate').toDateFormat('dd-mmm-yyyy');
            dataParams.doctorid = pageParams.rescheduleDoctorID.toString();
            dataParams.stype = $("#rescheduleAppType").val();
            dataParams.userid = localStorage["AxpertConnectUser"];
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                $(".rsTimes").html('No slots available');
                $("#rescheduleTime").val("");
            }
            else{
                $("#rescheduleTime").val("");
                $(".rsTimes").click(function () {
                    if ($(this).text() == "No slots available") {
                        $("#rescheduleTime").val("");
                    }
                    else {
                        $("#rescheduleTime").val($(this).text().trim());
                        $("#rescheduleTokenno").val($(this).attr('data-slno'));
                    }
                });                
            }
        }
    }
    else if (dataOptions.dataId == "op_patientlist_div1") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opdate = $("#my-datepicker").attr("data-val");
            dataParams.userid = localStorage["AxpertConnectUser"];
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                $(".details").html("");
            }
            else{
                $(".details").each(function () {
                    doRepeaterLoad($(this), data);
                });
                $(".details:contains('{{')").html("");
            }
        }
    }
    else if (dataOptions.dataId == "op_patientlist_div2") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opdate = $("#my-datepicker").attr("data-val");
            dataParams.userid = localStorage["AxpertConnectUser"];
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                $(".pastDetails").html("");
            }
            else{
                $(".pastDetails").each(function () {
                    doRepeaterLoad($(this), data);
                });
                $(".pastDetails:contains('{{')").html("");
            }
        }
    }
    else if (dataOptions.dataId == "op_patientlist_div3") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opdate = $("#my-datepicker").attr("data-val");
            dataParams.userid = localStorage["AxpertConnectUser"];
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                $(".visitHistroyDetails").html("");
            }
            else{
                $(".visthis").each(function () {
                    doRepeaterLoad($(this), data);
                });

                $(".visitHistroyDetails").each(function () {
                    if ($(this).find('.visthis').length == 0) {
                        $(this).hide();
                    }
                })
            }
        }
    }
    else if (dataOptions.dataId == "op_patientlist_reviewdelay") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.opdate = $("#my-datepicker").attr("data-val");
            dataParams.doctorid = pageParams.doctorid;
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if (data == "" || data.length == 0) {
                $(".reviewDelay").html("");
            }
            else {
                $(".reviewDelay").each(function () {
                    doRepeaterLoad($(this), data);
                });
                $(".reviewDelay:contains('{{')").html("");
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
    else if (dataOptions.dataId == "op_user_mobiledetails") {
        dataOptions.beforeLoad = function (dataOptions) {
            let dataParams = {};
            dataParams.uhid = dataOptions.elem.data("uhid");
            dataOptions.dataObj.dataParams = dataParams;
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if(data == "" || data.length == 0){
                dataOptions.elem.html(dataOptions.elem.html().toString().replaceAll(/{{.*}}/ig, "NA"));
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
    return dataOptions;
}

function beforePageParams(pageId, target, currElem, targetPageParams) {
    if (pageId == "op_patientdashboard" || pageId == "op_teleconsultation_patientdashboard") {
        targetPageParams += "&opdate=" + $("#my-datepicker").attr("data-val");
        targetPageParams += "&apmt_date=" + $("#my-datepicker").attr("data-val");
    }
    return targetPageParams;
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

function onInitAssessmentClose() {
    if (pageParams.status == "Completed" || pageParams.initAssessmentParams.status == "Completed") {
        $('#initialAssessmentModal').modal('hide');
    }
    else {
        confirmAlert({
            title: "Confirmation",
            message: "Do you want to close without saving the data?",
            yesCaption: "Save & Close",
            noCaption: "Close without saving",
            yesClick: function () {
                document.getElementById("initial-ass").contentWindow.document.querySelector("button[id='btnIASave']").click()
            },
            noClick: function () {
                $('#initialAssessmentModal').modal('hide');
            }
        })
    }
}
function generateSaveDataOptions(saveDataOptions) {
    if (saveDataOptions.formId == "op_set_doctorstatus") {
        saveDataOptions.saveJson = function () {
            var saveJson = {};
            saveJson.doctorstatus = $("#onlineStatus").find("option:selected").val().toString();
            saveJson.userid = localStorage["AxpertConnectUser"];
            return saveJson;
        }
    } else if (saveDataOptions.formId == "op_cancel_appointment") {
        saveDataOptions.afterSave = function () {
            $('#cancelAppointment').modal('hide');
            messageAlert({
                title: "Cancellation Message",
                message: "Appointment has been cancelled.",
                onclose: function () {
                    $(".loadCalendarData-dbdata").click();
                }
            })
        };

        saveDataOptions.saveJson = function (currJson) {
            var saveJson = {};
            saveJson = generateSaveJSON('op_cancel_appointment');
            return saveJson;
        }

    } else if (saveDataOptions.formId == "op_reschedule_appointment") {
        saveDataOptions.afterSave = function () {
            $("#rescheduleAppointment").modal("toggle");
            $("#rescheduleAppointmentConfirmation").modal("show");
        };
        saveDataOptions.beforeSave = function (saveDataOptions) {
            $('#rescheduleAppointmentConfirmationMsg').html("Patient " + $(event.target).data('patient') +
                " appointment on " + $("#my-datepicker").attr("data-val") + ", " + $(event.target).data(
                    'appointmenttime') + ", is rescheduled to " + $("#rescheduleDate").val() + ", " + $(
                        "#rescheduleTime").val());

        };
        saveDataOptions.saveJson = function (currJson) {
            var saveJson = {};
            saveJson = generateSaveJSON('op_reschedule_appointment');
            return saveJson;
        }
        saveDataOptions.validateSave = function () {
            if ($('input#rescheduleDate').val() == '') {
                ToastMaker('Date is Mandatory');
                return false;
            }
            if ($('input#rescheduleTime').val() == '') {
                ToastMaker('Slot time is mandatory');
                return false;
            }
            return true;
        };
    }

    return saveDataOptions;
}


function closeModal() {
    $(this).closest('.modal').modal("hide");
}

function generateLoadHTMLOptions(loadHTMLOptions) {
    if (loadHTMLOptions.pageId == "op_patientdashboard") {
        loadHTMLOptions.beforeLoad = function (loadHTMLOptions) {
            if($(loadHTMLOptions.currElem).hasClass('TC')){
                loadHTMLOptions.pageId = "op_teleconsultation_patientdashboard";
            }
            return loadHTMLOptions;
        };
    }

    return loadHTMLOptions;
    
}

function enableDisableEmrActions() {
    return;
    var selectedDate = $("#my-datepicker").attr("data-val");

    if (getDayDiff(selectedDate) < 0) {
        //Missed - Block all except reschedule & cancel for one day.
        $("tr.YetToVisit  + tr .openCons, tr.YetToVisit + tr .openIA, tr.YetToVisit + tr .openTC").addClass('ax-disabled');
    }

    if (getDayDiff(selectedDate) < -1) {
        //Missed - Block all except reschedule & cancel for one day.
        $(".openRS.YetToVisit, .openCancel.YetToVisit").addClass('ax-disabled');

        //Previous day appointment missed start consultation -- Should allow all the activity that are kept for Start consultation or Teleconsultation for only one day
        $("tr.Waiting  + tr .openCons, tr.Waiting + tr .openIA, tr.Waiting + tr .openTC").addClass('ax-disabled');
    }

    if (getDayDiff(selectedDate) < -7) {
        //Previous day appointment - Onhold: Should allow all the activity that are kept only for On - Hold for Upto One week
        $("tr.Onhold + tr .openCons, tr.Onhold + tr .openIA, tr.Onhold + tr .openTC").addClass('ax-disabled');
    }

    if (getDayDiff(selectedDate) > 0) {
        $(".openCons, .openIA, .openTC").addClass('ax-disabled');
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