﻿// Write your JavaScript code.

(function () {
    var viewModel = {
        files: [],
        uploadedFiles: [],
        notSupported: false,
        toAzure: true
    };

    function init() {
        initRivet();

        var flow = new Flow({
            target: '/api/Files/upload',
            chunkSize: 1024 * 1024 * 20, // 20MB
            testChunks: false,
            // simultaneousUploads: 5,
            maxChunkRetries: 5,
            chunkRetryInterval: 200,
            query: function () {
                return { toAzure: viewModel.toAzure };
            }
        });

        // Flow.js is not support
        if (!flow.support) {
            viewModel.notSupported = true;
            return;
        }

        getUploadedFiles();

        initBrowseButtons(flow);
        initDragDrop(flow);
        initFlowEvents(flow);
        initControlButtons();
    }

    function initBrowseButtons(flow) {
        flow.assignBrowse($('#btn-browse-files')[0]);
        flow.assignBrowse($('#btn-browse-folders')[0], true);
        flow.assignBrowse($('#btn-browse-images')[0], false, false, { accept: 'image/*' });
    }

    function initControlButtons() {
        $('#btn-delete-files').click(deleteFiles);
        $('#btn-clear').click(function () {
            viewModel.files = [];
        });
    }

    function initDragDrop(flow) {
        var $stage = $('#uploader-stage');
        var stage = $stage[0];

        flow.assignDrop(stage);

        stage.ondragenter = function () {
            $stage.addClass('file-dragover');
        };

        stage.ondragend = function () {
            $stage.removeClass('file-dragover');
        };

        stage.ondrop = function () {
            $stage.removeClass('file-dragover');
        };
    }

    function initFlowEvents(flow) {
        flow.on('fileAdded', onFileAdded);
        flow.on('fileSuccess', onFileUploadSuccess);
        flow.on('fileProgress', onFileUploadProgress);
        flow.on('filesSubmitted', function (file) {
            flow.upload();
        });
        
    }

    function onFileAdded(flowFile) {
        viewModel.files.push({
            uid: flowFile.uniqueIdentifier,
            name: flowFile.name,
            displaySize: readablizeBytes(flowFile.size),
            sizeInBytes: flowFile.size,
            percentUploaded: 0
        });
    }

    function onFileUploadSuccess(flowFile, message) {
        $.post('/api/files/aggregate/' + flowFile.uniqueIdentifier, {})
            .done(function (file) {
                // remove from the progress list
                for (var i = 0; i < viewModel.files.length; i++) {
                    if (viewModel.files[i].uid === flowFile.uniqueIdentifier) {
                        viewModel.files.splice(i, 1);
                    }
                }

                addToFileList(file);
            });
    }

    function onFileUploadProgress(flowFile) {
        var targetFile;

        for (var i = 0; i < viewModel.files.length; i++) {
            if (viewModel.files[i].uid === flowFile.uniqueIdentifier) {
                targetFile = viewModel.files[i];
                break;
            }
        }

        targetFile.percentUploaded = Math.floor(flowFile.progress() * 100);
    }

    function getUploadedFiles() {
        $.get('/api/files').done(function (files) {
            if (!files) {
                return;
            }

            viewModel.uploadedFiles = [];

            for (var i = 0; i < files.length; i++) {
                addToFileList(files[i]);
            }
        });
    }

    function deleteFiles() {
        var selectedFileIds = [];
        var selectedFiles = [];

        for (var i = 0; i < viewModel.uploadedFiles.length; i++) {
            var file = viewModel.uploadedFiles[i];
            if (file.selected) {
                selectedFileIds.push(file.id);
            }
        }

        if (selectedFileIds.length === 0) {
            return;
        }

        $.ajax({
                url: '/api/files',
                method: 'DELETE',
                data: { fileIds: selectedFileIds }
            })
            .done(function () {
                for (var i = viewModel.uploadedFiles.length - 1; i >= 0; i--) {
                    if (viewModel.uploadedFiles[i].selected) {
                        viewModel.uploadedFiles.splice(i, 1);
                    }
                }
            });
    }

    function addToFileList(file) {
        file.displaySize = readablizeBytes(file.size);
        file.fileImageClass = file.store === 2 ? 'glyphicon glyphicon-cloud' : '';
        viewModel.uploadedFiles.push(file);
    }

    function initRivet() {
        rivets.configure({
            prefix: 'rv'
        });

        rivets.formatters.width = function (value) {
            return 'width: ' + value + '%;';
        };

        rivets.formatters.bootstrapSuccess = function (value) {
            return value === true ? 'success' : '';
        };

        rivets.formatters.timeAgo = function (value) {
            return moment(value).fromNow();
        };

        rivets.bind($('#upload-container'), viewModel);
    }

    function readablizeBytes(bytes) {
        var s = ['bytes', 'kB', 'MB', 'GB', 'TB', 'PB'];
        var e = Math.floor(Math.log(bytes) / Math.log(1024));

        return (bytes / Math.pow(1024, e)).toFixed(2) + " " + s[e];
    }

    init();
})();