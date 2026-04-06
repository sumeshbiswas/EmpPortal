$(document).ready(() => {
    console.log('ready');

    console.log(jsSampleqc.$tblData); // You can now log the $tblData after it's defined
});

// Define the $tblData variable outside of jQuery


const jsSampleqc = {


    rowData: {
        RowIndex: 0,
        QC_CODE: 0,
        QCP_CODE: 0,
        Parameter: '',
        Unit: '',
        Level: '',
        AllowAmt: '',
        DeductAmt: 0,
        DeductNarr: 0,
        Items: []
    },

    TableRows: [],

    $tblData: $(`
            <table id="tblSampleQC" class="table-width">
                <thead class="table-head">
                    <tr>
                        <th style="display: none;">Qc id</th>
                        <th>Qcid</th>
                        <th>Qcpid</th>
                        <th>Parameter</th>
                        <th>Unit</th>
                        <th>Level</th>
                        <th>Allow amt</th>
                        <th>Dedu amt</th>
                        <th>Dedu narr</th>
                    </tr>
                </thead>
                <tbody>
                </tbody>
            </table>
        `),

    //Addcolumn: (name, thvalue) => {

    //    const $tblColumn = this.$tblData.find('thead');
    //    $tblColumn.append($('<th/>',
    //        {
    //            'name': name,
    //            'value': thvalue
    //        }
    //    ));
    //},

    //AddRow: () => {
    //    const $tblRowBody = this.$tblData.find('tbody');
    //    $tblRowBody.append($('<tr/>'));
    //},


    //AddTableRowData: (data) => {
    //    const $tblRowBody = this.$tblData.find('tbody tr:last');
    //    $tblRowBody.append($('<td/>', { 'text': data }));
    //},

    getRowdata: ($tblData) => {

        //const SampleQcData = [];
        //$('#tblSampleQC tbody tr').each(function (index) {

        jsSampleqc.TableRows = [];

        $tblData.find('tbody tr').each(function (index) {
            const $row = $(this);
            jsSampleqc.rowData = {
                RowIndex: index + 1,
                QC_CODE: $row.find('td:eq(1)').text().trim(),
                QCP_CODE: $row.find('td:eq(2)').text().trim(),
                Parameter: $row.find('td:eq(3)').text().trim(),
                Unit: $row.find('td:eq(4)').text().trim(),
                Level: $row.find('td:eq(5)').text().trim(),
                AllowAmt: $row.find('input[name="AllowAmt"]').val(),
                DeductAmt: $row.find('input[name="DeductAmt"]').val(),
                DeductNarr: $row.find('input[name="DeductNarr"]').val(),
                Items: []
            };

            $row.find('input[name^="Item_"]').each(function () {
                const inputName = $(this).attr('name');
                const value = $(this).val();
                const itemCodeMatch = inputName.match(/^Item_(.+)$/);
                if (itemCodeMatch) {
                    const itemCode = itemCodeMatch[1];
                    jsSampleqc.rowData.Items.push({ name: '' + [itemCode] + '', value: '' + value + '' });
                }
            });

            jsSampleqc.TableRows.push(jsSampleqc.rowData);

            //SampleQcData.push(this.rowData);
            //return this.rowData;

        });
    },


    AddNewQcCodeRows: ($data) => {
        $data.forEach(function (d, index) {
            let IsExistQcCode = jsSampleqc.TableRows.find(dr => dr.QCP_CODE === d.qcP_CODE) !== undefined;

            if (!IsExistQcCode || jsSampleqc.TableRows.length == 0) {
                // Add new row logic here, e.g.:
                jsSampleqc.TableRows.push({
                    QC_CODE: d.qC_CODE,
                    QCP_CODE: d.qcP_CODE,
                    Parameter: d.parameter,
                    Unit: d.unit,
                    Level: '',
                    AllowAmt: 0,
                    DeductAmt: 0,
                    DeductNarr: '',
                    Items: { name: d.item_Name, value: 0, Itemcode: d.item_Code }
                });

                let rowHtml = `
                        <tr>
                            <td style="display:none;">${d.item_Code || ''}</td>
                            <td>${d.qC_CODE || ''}</td>
                            <td>${d.qcP_CODE || ''}</td>
                            <td>${d.parameter || ''}</td>
                            <td>${d.unit || ''}</td>
                            <td>${d.qcP_STD || ''}</td>
                            <td><input type="number" class="form-control" name="AllowAmt" /></td>
                            <td><input type="number" class="form-control" name="DeductAmt" /></td>
                            <td><input type="text" class="form-control" name="DeductNarr" /></td>
                        </tr>`;

                jsSampleqc.$tblData.find('tbody').append(rowHtml);

            }
        });
    },


    colorItemRows: () => {

        const colorPalette = ['#d4edda', '#fff9c4', '#f28b82', '#f8bbd0', '#ce93d8', '#81d4fa', '#ffcc80'];
        const qcCodeColorMap = {};
        let qcColorIndex = 0;

        jsSampleqc.$tblData.find('tbody tr').each(function (index) {
            const $row = $(this);
            const qcCode = $row.find('td:eq(1)').text().trim()

            $row.find('input[name^="Item_"]').each(function () {

                if (!Object.prototype.hasOwnProperty.call(qcCodeColorMap, qcCode)) {
                    qcCodeColorMap[qcCode] = colorPalette[qcColorIndex % colorPalette.length];
                    qcColorIndex++;
                }

                const dynamicTDColor = qcCodeColorMap[qcCode];

                $(this).parent('td').css('background-color', dynamicTDColor);

            });
        });

    },

    removeItemForFill: (itemList) => {
        jsSampleqc.$tblData.find('tbody tr').each(function (index) {
            const $row = $(this);

            $row.find('input[name^="Item_"]').each(function () {
                const inputName = $(this).attr('name');
                //const value = $(this).val();
                const itemCodeMatch = inputName.match(/^Item_(.+)$/);
                // itemcodematch exist in itemlist array remove from there
                const itemCode = itemCodeMatch[1];
                itemList = itemList.filter(code => code.itemCode !== itemCode);
                console.log("Updated itemList:", itemList);
            });
        });
        return itemList;
    },

    setAnotherColumnRow: () => {

        const rows = $('#itemTable tbody tr');
        if (rows.length === 0) {
            //alert('No item data found.');
            return;
        }
        let payloadArray = [];
        rows.each(function () {
            const itemCode = $(this).find('td:first').text().trim();
            const itemName = $(this).find('td:eq(1)').text().trim();
            if (itemCode) payloadArray.push({ itemCode, itemName });
        });

        const dataJson = JSON.stringify(payloadArray);
        $.ajax({
            url: '/SampleQC/GetItemQCPDetails',
            type: 'POST',
            contentType: 'application/json',
            data: dataJson,
            success: function (response) {
                setRowsReadOnly(response);
            },
            error: function (xhr) {
                console.error('Error:', xhr.responseText);
            }
        });


        const setRowsReadOnly = (data) => {
            const headerlen = jsSampleqc.$tblData.find('thead tr th').length;
            const rowlen = jsSampleqc.$tblData.find('tbody tr:first td').length

            if (headerlen > rowlen) {
                const diff = headerlen - rowlen;

                for (let i = 0; i < diff; i++) {
                    const $tblrows = jsSampleqc.$tblData.find('tbody tr')
                    $tblrows.each(function (index, item) {
                        // Get QCP Code from current row (3rd column)
                        const rowQcpCode = $(this).find('td:eq(2)').text().trim();

                        // Get the corresponding column's item code from table header
                        const columnItem = jsSampleqc.$tblData.find('thead tr th').eq(i + 9).attr('id');

                        // Find item data in your data array
                        const itemData = data.find(d => d.itemCode.toString() === columnItem);

                        // Check if this QCP code & Item combination exists in data
                        const hasQcpMatch = data.some(a => a.qcpid.toString() === rowQcpCode && a.itemCode.toString() === columnItem);

                        // Check if it's the first row
                        const isFirstRow = index === 0;

                        // Allow editing only for first row or when QCP code matches
                        const isEditable = isFirstRow || hasQcpMatch;

                        // Set readonly attribute accordingly
                        const readonly = isEditable ? '' : 'readonly';

                        // let itemcode = jsSampleqc.$tblData.find('thead tr th:last').attr("id");
                        debugger;
                        let itemrow = jsSampleqc.TableRows.find(d => d.QCP_CODE == rowQcpCode);
                        if (itemrow.Items.length > 0) {
                            itemrow = jsSampleqc.TableRows.find(d => d.QCP_CODE == rowQcpCode && d.Items.some(item => item.name == columnItem));
                        }
                        
                        let itemvalue = "0";
                        console.log('itemrow= ?',itemrow)
                        if (itemrow != undefined && itemrow.Items != undefined) {
                            try {
                                const foundItem = itemrow.Items.find(item => item.name == columnItem || item.Itemcode == columnItem);
                                itemvalue = foundItem?.value ?? 0;
                            } catch (e) {
                            }
                        }

                        const $input = $('<input>', {
                            class: 'form-control',
                            type: 'number',
                            value: itemvalue,
                            name: 'Item_' + columnItem
                        });

                        if (readonly != '') {
                            $input.attr(readonly, readonly);
                        }

                        const $td = $('<td>').append($input);
                        //$td.append($Input);
                        $(this).append($td);
                    });
                }
            }

            jsSampleqc.colorItemRows()
        }


    },

    setReadonlyForResultOnload: () => {

        const vType = jsSampleQC.getQueryParam('vType');
        const vNo = jsSampleQC.getQueryParam('id');

        if (vNo) {
            $.ajax({
                url: '/SampleQCList/GetAllItems',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ vNo, vType }),
                success: function (response) {
                    loadResultRows(response);
                },
                error: function (xhr) {
                    if (typeof toastr !== 'undefined') {
                        toastr.error('Error: ' + xhr.responseText);
                    } else {
                        console.error('Error:', xhr.responseText);
                    }
                }
            });
        }

        const loadResultRows = (data) => {
            const headerlen = jsSampleqc.$tblData.find('thead tr th').length;
            const diff = headerlen - 9; // Assuming first 9 columns are fixed and rest are dynamic item columns
            debugger;
            for (let i = 0; i < diff; i++) {
                const $tblrows = jsSampleqc.$tblData.find('tbody tr')
                $tblrows.each(function (index, item) {


                    // Get QCP Code from current row (3rd column)
                    const rowQcpCode = $(this).find('td:eq(2)').text().trim();

                    // Get the corresponding column's item code from table header
                    const columnItem = jsSampleqc.$tblData.find('thead tr th').eq(i + 9).attr('id');

                    // Find item data in your data array
                    const itemData = data.find(d => d.itemCode.toString() === columnItem);

                    // Check if this QCP code & Item combination exists in data
                    const hasQcpMatch = data.some(a => a.qcpid.toString() === rowQcpCode && a.itemCode.toString() === columnItem);

                    // Check if it's the first row
                    const isFirstRow = index === 0;

                    // Allow editing only for first row or when QCP code matches
                    const isEditable = isFirstRow || hasQcpMatch;

                    // Set readonly attribute accordingly
                    const readonly = isEditable ? '' : 'readonly';

                    // let itemcode = jsSampleqc.$tblData.find('thead tr th:last').attr("id");

                    const $input = $(this).find('td').eq(i + 9).find('input');

                    if (readonly != '') {
                        $input.attr(readonly, readonly);
                    }

                    //const $td = $('<td>').append($input);
                    ////$td.append($Input);
                    //$(this).append($td);

                });
            }
        }
    },

    Emptyrows: () => {

        jsSampleqc.$tblData.find('tbody').empty();
    },

    AddDataInjsonObj: () => {

        jsSampleqc.TableRows = [];

        const headerlen = jsSampleqc.$tblData.find('thead tr th').length;

        const $tblrows = jsSampleqc.$tblData.find('tbody tr');

        $tblrows.each(function (index, item) {
            let rowdata = jsSampleqc.rowData;

            rowdata.QC_CODE = $(this).find('td:eq(1)').text().trim();
            rowdata.QCP_CODE = $(this).find('td:eq(2)').text().trim();
            rowdata.Parameter = $(this).find('td:eq(3)').text().trim();
            rowdata.Unit = $(this).find('td:eq(4)').text().trim();
            rowdata.Level = $(this).find('td:eq(5)').text().trim();
            rowdata.AllowAmt = $(this).find('td:eq(6) input').val();
            rowdata.DeductAmt = $(this).find('td:eq(7) input').val();
            rowdata.DeductNarr = $(this).find('td:eq(8) input').val();

            for (let i = 9; i < headerlen; i++) {
                rowdata.Items = {
                    ItemCode: jsSampleqc.$tblData.find('thead tr th').eq(i).attr('id'),
                    Value: $(this).find('td').eq(i).find('input').val(),
                }
            }

            jsSampleqc.TableRows.push(jsSampleqc.rowData);
        });

    },
    FillDataFromJsonObj: () => {


    }



}
