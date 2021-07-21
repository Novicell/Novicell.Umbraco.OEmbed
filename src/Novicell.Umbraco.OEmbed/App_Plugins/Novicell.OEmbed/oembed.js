(function () {
  "use strict";

  var mapFromUmbracoSettings = function (settingsKey) {
    var getSetting = function (key, umbracoSettings) {
      return (umbracoSettings || Umbraco.Sys.ServerVariables.umbracoSettings)[
        key
      ];
    };
    return function (p, umbracoSettings) {
      return getSetting(settingsKey, umbracoSettings) + p;
    };
  };

  var mapFromAppPluginsPath = mapFromUmbracoSettings("appPluginsPath");
  var mapFromUmbracoPath = mapFromUmbracoSettings("umbracoPath");

  var previewController = function ($sce, $window) {
    var vm = this;

    vm.aspectRatio = null;

    var getSrcDoc = function (html, aspectRatio) {
      if (!html) return null;

      var $doc = $window.angular.element("<div>", { html: html });

      if (aspectRatio > 0) {
        $doc.css({
          width: "100%",
          height: "100%",
        });
      }

      var srcdoc = [
        "<html><head>",
        "</head><body>",
        $doc.html(),
        "</body></html>",
      ];

      if (aspectRatio > 0)
        srcdoc.splice(
          1,
          0,
          "<style>html,body,iframe{width:100%;height:100%;}</style>"
        );

      return $sce.trustAsHtml(srcdoc.join(""));
    };

    vm.observeFrame = function (frame) {
      function createMutationObserver(document, callback) {
        var target = document.querySelector("body"),
          config = {
            attributes: true,
            attributeOldValue: false,
            characterData: true,
            characterDataOldValue: false,
            childList: true,
            subtree: true,
          },
          observer = new MutationObserver(callback);

        observer.observe(target, config);
      }

      var contentWindow = frame.contentWindow;

      var updateFrameSize = function (width, height) {
        //frame.width = width;
        frame.style.height = height + "px";
      };

      createMutationObserver(
        contentWindow.document,
        _.debounce(
          function (/*mutations*/) {
            updateFrameSize(
              contentWindow.document.body.scrollWidth,
              contentWindow.document.body.scrollHeight
            );
          },
          1
        )
      );
    };

    var round = function (n, d) {
      var m = Math.pow(10, d);
      return Math.round(n * m) / m;
    };

    vm.$onChanges = function (/*changes*/) {
      // we only get updates on the model (oembed obj)

      if (vm.model) {
        vm.aspectRatio =
          isNaN(vm.model.width) || isNaN(vm.model.height)
            ? null
            : round(vm.model.height / vm.model.width, 4);

        vm.srcdoc = getSrcDoc(vm.model.html, vm.aspectRatio);
      } else {
        vm.srcdoc = null;
        vm.aspectRatio = null;
      }
    };
  };

  var previewComponent = {
    transclude: true,
    bindings: {
      model: "<",
    },
    bindToController: true,
    controller: previewController,
    controllerAs: "vm",
    templateUrl: function () {
      return mapFromAppPluginsPath("/Novicell.OEmbed/oembed.preview.html");
    },
  };

  //<novicell-oembed-preview model=value.oembed />
  angular
    .module("umbraco.directives")
    .component("novicellOembedPreview", previewComponent);

  var propertyEditorController = function (
    $scope,
    editorService,
    angularHelper
  ) {
    var vm = this;

    vm.add = function () {
      openOEmbedEditor(null);
    };

    vm.remove = function () {
      $scope.model.value = null;
      setDirty();
    };

    vm.edit = function () {
      openOEmbedEditor(_.clone($scope.model.value));
    };

    function setDirty() {
      angularHelper.getCurrentForm($scope).$setDirty();
    }

    function openOEmbedEditor(value) {
      var editor = {
        view: mapFromAppPluginsPath("/Novicell.OEmbed/oembed.editor.html"),
        size: "small",
        value: value,
        config: $scope.model.config,
        submit: function (model) {
          $scope.model.value = model.value;
          editorService.close();
          setDirty();
        },
        close: function () {
          editorService.close();
        },
      };

      editorService.open(editor);
    }
  };

  angular
    .module("umbraco")
    .controller(
      "Novicell.OEmbed.PropertyEditorController",
      propertyEditorController
    );

  var editorController = function (
    $scope,
    $http,
    $window,
    $q,
    $sce,
    formHelper,
    localizationService
  ) {
    var vm = this;

    // public
    vm.preview = null;

    vm.submit = function () {
      if ($scope.model && $scope.model.submit) {
        $scope.model.submit($scope.model);
      }
    };

    vm.close = function () {
      if ($scope.model && $scope.model.close) {
        $scope.model.close();
      }
    };

    vm.update = function () {
      var args = { scope: $scope, skipValidation: true };
      if (!formHelper.submitForm(args)) {
        return;
      }

      var url = $scope.model.value.url;

      if (angular.isString(url) && url !== "") {
        fetchOEmbed(url, $scope.model.config).then(
          function (r) {
            $scope.model.value.oembed = r.data;
          },
          function (r) {
              console.log('fetchOEmbed-error', r);
            formHelper.handleError(r);
            $scope.model.value.oembed = null;
          },
          function () {}
        );
      } else {
        $scope.model.value.oembed = null;
      }
    };

    vm.validateMandatory = function () {
      return {
        isValid:
          !$scope.model.validation.mandatory ||
          isValidMandatory($scope.model.value),
        errorMsg: "Value cannot be empty",
        errorKey: "required",
      };
    };

    // private
    var isValidMandatory = function (value) {
      return (
        angular.isObject(value) &&
        angular.isString(value.url) &&
        angular.isObject(value.oembed)
      );
    };

    var fetchOEmbed = function (url, config) {
      var deferred = $q.defer();

      vm.loading = true;

      var requestConfig = {
        cache: true,
        umbIgnoreErrors: true,
        params: angular.extend({}, config, { url: url }),
      };

      var _url = mapFromUmbracoPath("/backoffice/novicell/oembed/Get");

      $http.get(_url, requestConfig).then(
        function (a, b, c, d) {
          deferred.resolve(a, b, c, d);
          vm.loading = false;
        },
        function (a, b, c, d) {
          deferred.reject(a, b, c, d);
          vm.loading = false;
        }
      );

      return deferred.promise;
    };

    vm.$onInit = function () {
      if (!$scope.model.title) {
        localizationService.localize("general_embed").then(function (value) {
          $scope.model.title = value;
        });
      }
    };
  };

  angular
    .module("umbraco")
        .controller("Novicell.OEmbed.EditorController", editorController);

    angular
        .module('umbraco.resources')
        .factory('novicellOEmbedResourse',
            function ($q, $http, umbRequestHelper) {

                var _url = mapFromUmbracoPath("/backoffice/novicell/oembed/");


            // the factory object returned
            return {
                // this calls the ApiController we setup earlier
                getOEmbed: function (url) {
                    return umbRequestHelper.resourcePromise(
                        $http.get( _url + "Get", {params:{'url':url}} ),
                        "Failed to retrieve OEmbed");
                }
            };
        }
    );

})();
