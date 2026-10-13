var viewDocParams = {};
$(document).ready(function () {
    debugger;
    if (typeof window.parent.pageParams.patientDetails != "undefined") {
        viewDocParams = Object.assign({}, window.parent.pageParams.patientDetails);
    }
    else {
        viewDocParams = Object.assign({}, window.parent.pageParams.docParams);
    }
});

function generateLoadDataOptions(dataOptions) {
    if (dataOptions.dataId == "op_doctor_docs" || dataOptions.dataId == "op_patient_docs") {
        dataOptions.beforeLoad = function (dataOptions) {
            dataOptions.dataObj.dataParams = new Object();
            dataOptions.dataObj.dataParams.opno = viewDocParams.opno;
            dataOptions.dataObj.dataParams.uhid = viewDocParams.uhid;

            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if (data == "" || data.length == 0) {
                dataOptions.elem.find('tr').html("No documents available")
            }
        }
    }

    return dataOptions;
}