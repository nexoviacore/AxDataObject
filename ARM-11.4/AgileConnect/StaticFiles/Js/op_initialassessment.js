var pageParams = new Object();
var loadDataObj;

$(document).ready(function () {

    loadAttachmentsEvents();
    setPageParams();
    loadInitalAssessment();

    if (typeof window.parent.pageParams.initAssessmentParams != "undefined") {
        pageParams = Object.assign({}, window.parent.pageParams.initAssessmentParams);
    } else
        pageParams = Object.assign({}, window.parent.pageParams);

    var uhId = pageParams.uhid;
    var iaId = pageParams.inititalassessmentid;

    pageParams.iaDoctorId = pageParams.doctorid;

    $("#iaUHID").val(uhId);
    $("#recordid").val(iaId);
    $("#doctorid").val(pageParams.doctorid);

    if (pageParams.inititalassessmentid == "0") {
        $("#addFamilyHis").click().click();
        $.when(isRowAPIsLoaded())
            .then(function () {
                $("[id^=selectFamilyMember_]:eq(1)").val('Father');
                $("[id^=selectFamilyMember_]:eq(2)").val('Mother');
            })

        $("#addAllergicHis").click();
        $("#addSocialHis").click();
        $("#addSurgeryHis").click();
        $("#addMedHis").click();
    }

    loadAPIData();

    if (pageParams.status == "Completed") {
        $(".tab-content").addClass("ax-disabled-modal");
        $(".add-new-complaints").addClass("ax-disabled");
        $("#btnIASave").hide();
    }

    enableDisableEmrActions();
});

function enableDisableEmrActions() {
    return;
    var selectedDate = window.parent.pageParams.opdate;

    if (getDayDiff(selectedDate) < -1 && window.parent.pageParams.status == "Waiting") {
        //Previous day appointment missed start consultation -- Should allow all the activity that are kept for Start consultation or Teleconsultation for only one day
        $(".tab-content").addClass("ax-disabled-modal");
        $(".add-new-complaints").addClass("ax-disabled");
        $("#btnIASave").hide();
    }

    if (getDayDiff(selectedDate) < -7 && window.parent.pageParams.status == "Onhold") {
        //Previous day appointment - Onhold: Should allow all the activity that are kept only for On - Hold for Upto One week
        $(".tab-content").addClass("ax-disabled-modal");
        $(".add-new-complaints").addClass("ax-disabled");
        $("#btnIASave").hide();
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

function loadAttachmentsEvents() {
    // file upload
    var $group = $('.input-group');
    var $file = $group.find('input[type="file"]')
    var $browse = $group.find('[data-action="browse"]');
    var $fileDisplay = $group.find('[data-action="display"]');
    var $reset = $group.find('[data-action="reset"]');
    var resetHandler = function (e) {
        if ($file.length === 0) {
            return;
        }
        $file[0].value = '';
        if (!/safari/i.test(navigator.userAgent)) {
            $file[0].type = '';
            $file[0].type = 'file';
        }
        $file.trigger('change');
    };
    var browseHandler = function (e) {
        //If you select file A and before submitting you edit file A and reselect it it will not get the latest version, that is why we  might need to reset.
        //resetHandler(e);
        $file.trigger('click');

    };
    $browse.on('click', function (e) {
        if (event.which != 1) {
            return;
        }
        browseHandler();
    });
    $fileDisplay.on('click', function (e) {
        if (event.which != 1) {
            return;
        }
        browseHandler();
    });
    $reset.on('click', function (e) {
        if (event.which != 1) {
            return;
        }
        resetHandler();
    });

    $file.on('change', function (e) {
        var files = [];
        if (typeof e.currentTarget.files) {
            for (var i = 0; i < e.currentTarget.files.length; i++) {
                files.push(e.currentTarget.files[i].name.split('\\/').pop())
            }
        } else {
            files.push($(e.currentTarget).val().split('\\/').pop())
        }
        $fileDisplay.val(files.join('; '))
    });

    // File Upload
    const dt = new DataTransfer();
    const dt2 = new DataTransfer();
    const dt3 = new DataTransfer();

    $("#attachment").on('change', function (e) {
        for (var i = 0; i < this.files.length; i++) {
            let fileBloc = $('<span/>', {
                    class: 'file-block'
                }),
                fileName = $('<span/>', {
                    class: 'name',
                    text: this.files.item(i).name
                });
            fileBloc.append('<span class="file-delete"><span>+</span></span>')
                .append(fileName);
            $("#filesList > #files-names").append(fileBloc);
        };

        for (let file of this.files) {
            dt.items.add(file);
        }

        this.files = dt.files;

        $('span.file-delete').click(function () {
            let name = $(this).next('span.name').text();
            $(this).parent().remove();
            for (let i = 0; i < dt.items.length; i++) {
                if (name === dt.items[i].getAsFile().name) {
                    dt.items.remove(i);
                    continue;
                }
            }
            document.getElementById('attachment').files = dt.files;
        });
    });

    $("#attachmentPrescription").on('change', function (e) {
        for (var i = 0; i < this.files.length; i++) {
            let fileBloc = $('<span/>', {
                    class: 'file-block'
                }),
                fileName = $('<span/>', {
                    class: 'name',
                    text: this.files.item(i).name
                });
            fileBloc.append('<span class="file-delete"><span>+</span></span>')
                .append(fileName);
            $("#filesList > #files-attachmentPrescription").append(fileBloc);
        };

        for (let file of this.files) {
            dt2.items.add(file);
        }

        this.files = dt2.files;

        $('span.file-delete').click(function () {
            let name = $(this).next('span.name').text();
            $(this).parent().remove();
            for (let i = 0; i < dt2.items.length; i++) {
                if (name === dt2.items[i].getAsFile().name) {
                    dt2.items.remove(i);
                    continue;
                }
            }
            document.getElementById('attachmentPrescription').files = dt2.files;
        });
    });

    $("#attachmentSurgeryDetails").on('change', function (e) {
        for (var i = 0; i < this.files.length; i++) {
            let fileBloc = $('<span/>', {
                    class: 'file-block'
                }),
                fileName = $('<span/>', {
                    class: 'name',
                    text: this.files.item(i).name
                });
            fileBloc.append('<span class="file-delete"><span>+</span></span>')
                .append(fileName);
            $("#filesList > #files-attachmentSurgeryDetails").append(fileBloc);
        };

        for (let file of this.files) {
            dt3.items.add(file);
        }

        this.files = dt3.files;

        $('span.file-delete').click(function () {
            let name = $(this).next('span.name').text();
            $(this).parent().remove();
            for (let i = 0; i < dt3.items.length; i++) {
                if (name === dt3.items[i].getAsFile().name) {
                    dt3.items.remove(i);
                    continue;
                }
            }
            document.getElementById('attachmentSurgeryDetails').files = dt3.files;
        });
    });

}

async function loadAPIData() {
    if (pageParams.inititalassessmentid != "0") {
        loadData({
            dataId: 'op_initialassessmentData',
            beforeLoad: function (dataOptions) {
                dataOptions.dataObj.dataParams.recid = pageParams.inititalassessmentid;
                return dataOptions;
            },
            afterLoad: function (data, dataOptions) {
                loadDataObj = generateLoadTstructDataObject(data);
                bindDcData(loadDataObj, ['1', '5', '4', '8', '7', '11', '13']);

                $.when(isAPIDataLoaded('op_complaintsList'))
                    .then(function () {
                        bindInitialAssessmentData(['DC2']);
                        //bindDcData(loadDataObj, ['2']);
                    })
            }
        });
    }
}

function loadInitalAssessment() {
    var btnFinish = $('<button></button>').text('Save')
        .addClass('btn btn-info sw-btn-group-extra d-none ax-save').attr('id', 'btnIASave').attr('data-target', 'op_initial_assessment_save')
    $('#initialAssessmentWizard').smartWizard({
        selected: 0, // Initial selected step, 0 = first step
        theme: 'dots', // theme for the wizard, related css need to include for other than default theme
        justified: true, // Nav menu justification. true/false
        darkMode: false, // Enable/disable Dark Mode if the theme supports. true/false
        autoAdjustHeight: true, // Automatically adjust content height
        cycleSteps: false, // Allows to cycle the navigation of steps
        backButtonSupport: true, // Enable the back button support
        enableURLhash: false, // Enable selection of the step based on url hash
        transition: {
            animation: 'none', // Effect on navigation, none/fade/slide-horizontal/slide-vertical/slide-swing
            speed: '400', // Transion animation speed
            easing: '' // Transition animation easing. Not supported without a jQuery easing plugin
        },
        toolbarSettings: {
            toolbarPosition: 'bottom', // none, top, bottom, both
            toolbarButtonPosition: 'center', // left, right, center
            showNextButton: true, // show/hide a Next button
            showPreviousButton: true, // show/hide a Previous button
            toolbarExtraButtons: [btnFinish] // Extra buttons to show on toolbar, array of jQuery input/buttons elements
        },
        anchorSettings: {
            anchorClickable: true, // Enable/Disable anchor navigation
            enableAllAnchors: false, // Activates all anchors clickable all times
            markDoneStep: true, // Add done state on navigation
            markAllPreviousStepsAsDone: true, // When a step selected by url hash, all previous steps are marked done
            removeDoneStepOnNavigateBack: false, // While navigate back done step after active step will be cleared
            enableAnchorOnDoneStep: true // Enable/Disable the done steps navigation
        },
        keyboardSettings: {
            keyNavigation: false
        },
        lang: { // Language variables for button
            next: 'Next',
            previous: 'Back'
        },
        disabledSteps: [], // Array Steps disabled
        errorSteps: [], // Highlight step with errors
        hiddenSteps: [] // Hidden steps
    }).on("leaveStep", function (e, anchorObject, stepNumber, stepDirection) {        
        try {
            let isValid = validateStep(stepNumber);
            if (isValid) {
                if (stepDirection == "2") //here is the final step: Note: 0,1,2
                {
                    $('.sw-btn-group-extra').removeClass('d-none');
                } else {
                    $('.sw-btn-group-extra').addClass('d-none');
                }
                return true;
            }
            else
                return false;
        } catch (ex) {};

        return true;
    });

    bindEvents();
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

function generateSaveDataOptions(saveDataOptions) {
    if (saveDataOptions.formId == "op_initial_assessment_save") {
        saveDataOptions.saveJson = function (currJson) {
            var saveJson = {};
            var rowno = tstGridDcRowNos['DC2'] || 0;
            rowno = rowno + 1;
            var dc2JsonArr = new Array();
            $("#op_initial_assessment_save #complaints .ax-savefld").each(function () {
                let $complaint = $(this);
                if ($complaint.hasClass('complaintsDropdown')) {
                    var dataArr = getFieldValue($complaint);
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
                } else if ($complaint.is(':checked')) {
                    var dc2Json = new Object();
                    dc2Json.rowno = rowno;
                    dc2Json.text = $complaint.data('rowid') || 0;
                    dc2Json.columns = {
                        "complaints": $complaint.val(),
                        "complaintsid": $complaint.attr('id'),
                        "opno_complaints": pageParams.opno,
                        "app_no_complaints": pageParams.appno
                    }
                    dc2JsonArr.push(dc2Json);
                    rowno++;
                }
            });
            saveJson['axp_recid2'] = dc2JsonArr;            
            currJson.recdata.push(saveJson)
            return currJson;
        };
        saveDataOptions.afterSave = function (data) {
            var recId = data.result[0].message[0].recordid;
            pageParams.inititalassessmentid = recId;
            window.parent.pageParams.inititalassessmentid = recId;

            messageAlert({
                title: "Confirmation",
                message: "Initial Assessment is saved.",
                onclose: function () {
                    window.parent.$('#initialAssessmentModal').modal('toggle');
                    //Calendar page refresh
                    if (window.parent.$(".loadCalendarData-dbdata").length > 0) {
                        window.parent.$(".loadCalendarData-dbdata").click();
                    }

                    //Dashboard page refresh
                    if (window.parent.$("#initialassessmentctrl_ref").length > 0) {
                        window.parent.$("#initialassessmentctrl_ref").click();
                        window.parent.$("#consultation-ref").click();
                    }
                }
            })
        }
    }
    else if (saveDataOptions.formId == "chiefcomplaint_save") {
        saveDataOptions.afterSave = function () {
            $("#addComplaint").val('');
            $('.modal-pop-newcomplaints').removeClass('is-visible');

            $('#complaintsSelect').each(function () {
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
    if (dataOptions.dataId == "op_initial_assessment_present_complaints") {
        dataOptions.beforeLoad = function (dataOptions) {
            dataOptions.dataObj.dataParams = new Object();
            dataOptions.dataObj.dataParams.doctorid = pageParams.iaDoctorId.toString();
            return dataOptions;
        };

        dataOptions.afterLoad = function (data, dataOptions) {
            if (data == "" || data.length == 0) {
                dataOptions.elem.html('');
            }            
        };

    } else if (dataOptions.dataId == "op_complaintsList") {
        dataOptions.afterLoad = function (data) {
            //$(".js-select2").select2({
            //    closeOnSelect: false,
            //    placeholder: "Select Complaints",
            //    dropdownParent: $("#initialAssessmentModal")
            //});
        }
    } else if (dataOptions.dataId == "op_documentType") {
        dataOptions.afterLoad = function (data) {
            $('.selectDocumentType').selectpicker('refresh');
        }
    } else if (dataOptions.dataId == "op_initial_assessment_allergylist") {
        dataOptions.beforeLoad = function (dataOptions) {
            dataOptions.dataObj.dataParams = new Object();
            dataOptions.dataObj.dataParams.allergytype = $("[id='" + dataOptions.clickedAllergyElem + "']").val();
            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if (data == "" || data.length == 0) {
                data = [{
                    'allergyname': 'NA'
                }]
            }

            createMagicSearch({
                elem: $("[id='" + dataOptions.clickedAllergyElem + "']").closest('tr').find('input[id^=selectAllergy_]'),
                dataSource: data,
                field: 'allergyname'
            });
        };
    }
    return dataOptions;
}

function bindInitialAssessmentData(loadDCArr) {
    if (typeof loadDataObj == "undefined" || loadDCArr.length == 0)
        return;

    if (loadDCArr.indexOf("DC2") > -1) {
        let valArr = [];
        let $complaints = $('#complaintsSelect');
        for (let row in loadDataObj.DC2) {
            let rowData = loadDataObj.DC2[row];
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

function generateCopyHtmlOptions(copyHtmlOptions) {
    if (copyHtmlOptions.sourceId == "allergicHisRow") {
        copyHtmlOptions.afterCopy = function (copyHtmlOptions) {
            var $newElem = $(copyHtmlOptions.newElem);
            var rowno = copyHtmlOptions.newRowNo.toString()

            $(".allergicHistory input[type=radio]").unbind('click').click(function (e) {
                loadData({
                    dataId: 'op_initial_assessment_allergylist',
                    clickedAllergyElem: $(this).attr('id')
                });
            })

            $(".allergicHistory input[name^=allergicHistory_]:checked").each(function () {
                if ($(this).val() == "N.K.D.A") {
                    $(".allergicHistory input[id='N.K.D.A_" + rowno + "'],.allergicHistory input[id='Drug_" + rowno + "'],.allergicHistory label[for='N.K.D.A_" + rowno + "'],.allergicHistory label[for='Drug_" + rowno + "']").addClass("ax-disabled");
                    return false;
                }
            });
            //if (typeof copyHtmlOptions.dataLoad == "undefined")
            //    $(".allergicHistory input[name=allergicHistory_" + rowno + "]:not(.ax-disabled)").first().click();
        }
    } else if (copyHtmlOptions.sourceId == "socialHisRow") {
        copyHtmlOptions.afterCopy = function (copyHtmlOptions) {
            var $newElem = $(copyHtmlOptions.newElem);
            var rowno = copyHtmlOptions.newRowNo.toString()

            $newElem.find(".socialHistory input[type=radio]").unbind('click').click(function () {
                if ($(this).val() == 'Alcohol') {
                    $(this).closest('tr').find('label.alcoholml').show('');
                    $(this).closest('tr').find('div.socialType').addClass('ax-disabled').find('input[type=radio]').prop('checked', false);
                }
                else {
                    $(this).closest('tr').find('label.alcoholml').hide();
                    $(this).closest('tr').find('div.socialType').removeClass('ax-disabled');

                }
            })
        }
    }
    else if (copyHtmlOptions.sourceId == "surgeryHisRow") {
        copyHtmlOptions.afterCopy = function (copyHtmlOptions) {
            var $newElem = $(copyHtmlOptions.newElem);

            $newElem.find(".selectSurgeryYears").datepicker({
                format: "yyyy",
                viewMode: "years",
                minViewMode: "years",
                endDate: '+0d',
                autoclose: true
            });
        }
    }

    return copyHtmlOptions;
}

function validateStep(stepnumber) {
    var isStepValid = true;
    if (stepnumber == 1) {
        if (checkDuplicate("#famHisTable")) {
            isStepValid = false;
        }
        if (checkEmpty("#famHisTable")) {
            isStepValid = false;
        }
    }
    return isStepValid;
}

function generateSearchOptions(fldOptions) {
    let searchOptions = {};
    if (fldOptions.dataId == "op_medicineList") {
        searchOptions.onChange = function ($input, data) {
            $($input).closest('tr').find('td input.medicineid').val(data.itemid);
        }
    } else if (fldOptions.dataId == "op_initialassessment_surgery") {
        searchOptions.onChange = function ($input, data) {
            $($input).closest('tr').find('td input.surgeryid').val(data.surgeryid);
        }
    }
    return searchOptions;
}

$(document).ready(function () {
     $('.ax-select2').each(function () {
        let $elem = $(this);
        let dataId = $elem.attr('data-ax-datasource');
        if (typeof dataId != "undefined") {
            let select2Options = { refresh: false, ajaxData: false, dataId: dataId, elem: $elem };
            createSelect2(select2Options);
        }
    })

    $('.add-new-complaints').on('click', function (e) {
        $("#addComplaint").val('');
        e.preventDefault();
        $('.modal-pop-newcomplaints').toggleClass('is-visible');
    });
});

