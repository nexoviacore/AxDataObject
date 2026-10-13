var loginURL = "../api/Auth/login";

function Login(username, password, groupid) {
    this.username = username;
    this.password = password;
    this.groupid = groupid;
}

function CallLoginAPI() {

    var loginObj = new Login($("#username").val(), MD5($("#password").val()), $('#usergroup option:selected').val());

    $.ajax({
        type: "POST",
        url: loginURL,
        cache: false,
        async: true,
        contentType: "application/json;charset=utf-8",
        data: JSON.stringify(loginObj),
        dataType: "text",
        success: function (token) {
            //console.log(token);
            localStorage.setItem("RCP_JwtToken", "Bearer " + token);
            localStorage.setItem("AxpertConnectUser", $("#username").val());
            window.location.href = "home";
        },
        error: function (request, status, error) {
            alert("Invalid username/password.");
        }
    });
}


$(document).ready(function () {
    loadInitalAssessment();    
});

function loadInitalAssessment() {
    var btnFinish = $('<button></button>').text('Save')
        .addClass('btn btn-info sw-btn-group-extra d-none ax-save').attr('id', 'btnIASave').attr('data-target', 'initial_assessment_save')
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
                if (stepDirection == "1") //here is the final step: Note: 0,1,2
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
    return saveDataOptions;
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