(function () {
    var viewModel = {
        files: [],
        uploadedFiles: [],
        notSupported: false,
        toAzure: false,
        paused: false,
        deleteFiles: deleteFiles,
        downloadFiles: downloadFiles
    };

    function init() {
        initRivet();

        var flow = new Flow({
            target: '/api/Files/upload',
            chunkSize: 1024 * 1024 * 20, // 20MB
            testChunks: false,
            simultaneousUploads: 5,
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

        viewModel.pauseDownload = function () {
            viewModel.paused = !viewModel.paused;

            if (viewModel.paused) {
                flow.pause();
            } else {
                flow.resume();
            }
        };

        viewModel.stopDownload = function () {
            flow.cancel();

            // clear up all chunks after 2 secs
            setTimeout(function () {
                var fileUIds = _.map(viewModel.files, 'uid');

                $.post('/api/files/cancelUploads', { fileIds: fileUIds }).done(function () {
                    viewModel.files = [];
                });
            }, 2000);
        };
    }

    function initBrowseButtons(flow) {
        flow.assignBrowse($('#btn-browse-files')[0]);
        flow.assignBrowse($('#btn-browse-folders')[0], true);
        flow.assignBrowse($('#btn-browse-images')[0], false, false, { accept: 'image/*' });
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
        flow.on('error', function (message, file, chunk) {
            flow.pause();
            viewModel.paused = true;
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
                remove(viewModel.files, function (f) {
                    return f.uid === flowFile.uniqueIdentifier;
                });

                addToFileList(file);
            });
    }

    function onFileUploadProgress(flowFile) {
        var targetFile = _.find(viewModel.files, { uid: flowFile.uniqueIdentifier });

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
        var selectedFileIds = _.map(_.filter(viewModel.uploadedFiles, { selected: true }), 'id');

        if (selectedFileIds.length === 0) {
            return;
        }

        $.ajax({
            url: '/api/files',
            method: 'DELETE',
            data: { fileIds: selectedFileIds }
        })
        .done(function () {
            remove(viewModel.uploadedFiles, function (file) {
                return file.selected;
            });
        });
    }

    function downloadFiles(e) {
        e.preventDefault();

        var selectedFileIds = _.map(_.filter(viewModel.uploadedFiles, { selected: true }), 'id');
        var downloadUrl;

        if (selectedFileIds.length === 1) {
            downloadUrl = '/api/files/download/' + selectedFileIds[0];
        } else if (selectedFileIds.length > 1) {
            var ids = _.map(selectedFileIds, function (id) { return 'fileIds=' + id; });
            var downloadUrl = '/api/files/downloadAll?' + ids.join('&');
        }

        window.location.href = downloadUrl;
        resetFilesSelection();
    }

    function addToFileList(file) {
        file.displaySize = readablizeBytes(file.size);
        file.fileImageClass = file.store === 2 ? 'glyphicon glyphicon-cloud' : '';
        viewModel.uploadedFiles.push(file);
    }

    function resetFilesSelection() {
        _.forEach(viewModel.uploadedFiles, function (file) {
            file.selected = false;
        });
    }

    function remove(list, predicate) {
        for (var i = list.length - 1; i >= 0; i--) {
            if (predicate(list[i]) === true) {
                list.splice(i, 1);
            }
        }
    }

    function initRivet() {
        rivets.configure({
            prefix: 'rv'
        });

        rivets.formatters.width = function (value) {
            return 'width: ' + value + '%;';
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