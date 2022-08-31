	latDir = tile[0:1]
	latNumber = float(tile[1:3])
	longDir = tile[3:4]
	longNumber = float(tile[4:7])

	if latDir == "N":
		yMax = latNumber + 1
	elif latDir == "S":
		yMax = (latNumber - 1) * -1
	else:
		yMax = 0

	yMin = yMax - ( 1 * TilesPerMap )

	if longDir == "E":
		xMin = longNumber
	elif longDir == "W":
		xMin = longNumber * -1
	else:
		xMax = 0

	xMax = xMin + ( 1 * TilesPerMap )

	#create missing folders
	folder = path + 'image_exports/'
	if not os.path.exists(folder):
		os.makedirs(folder)
	folder = path + 'image_exports/' + tile + '/'
	if not os.path.exists(folder):
		os.makedirs(folder)

	##################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("heightmap_source"):
			layers.append(layer)
		elif layer.name().startswith("heightmap_land_polygons"):
			layers.append(layer)
		elif layer.name().startswith("heightmap_background"):
			layers.append(layer)

	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '.png', settings)
	assert ret==0

	##################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("slope"):
			layers.append(layer)

	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_slope.png', settings)
	assert ret==0

	##################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("land_polygons"):
			layers.append(layer)
		elif layer.name().startswith("bathymetry_source"):
			layers.append(layer)
		elif layer.name().startswith("background_bathymetry"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create missing folders
	folder = path + 'image_exports/'
	if not os.path.exists(folder):
		os.makedirs(folder)
	folder = path + 'image_exports/' + tile + '/'
	if not os.path.exists(folder):
		os.makedirs(folder)

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_bathymetry.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	#save all layers starting with "landuse" into a list
	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("landuse"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_landuse.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	#save all layers starting with "water" into a list
	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("water"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_water.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	if rivers == "small":
		for layer in QgsProject.instance().mapLayers().values():
			if layer.name().startswith("rivers_small"):
				layers.append(layer)

	if rivers == "medium":
		for layer in QgsProject.instance().mapLayers().values():
			if layer.name().startswith("rivers_medium"):
				layers.append(layer)

	if rivers == "large":
		for layer in QgsProject.instance().mapLayers().values():
			if layer.name().startswith("rivers_large"):
				layers.append(layer)

	if rivers == "majorMany":
		for layer in QgsProject.instance().mapLayers().values():
			if layer.name().startswith("MajorRiversMany"):
				layers.append(layer)

	if rivers == "majorFew":
		for layer in QgsProject.instance().mapLayers().values():
			if layer.name().startswith("MajorRiversFew"):
				layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_river.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	#save all layers starting with "water" into a list
	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("wet_glacier"):
			layers.append(layer)
		elif layer.name().startswith("wet_swamp"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_wet.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	#save all layers starting with "road" into a list
	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("road"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_road.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith(TerrainSource):
			layers.append(layer)
			
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_terrain.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	#save all layers starting with "climate" into a list
	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("climate"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_climate.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	#save all layers starting with "ECO_NAME" into a list
	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("wwf_terr_ecos"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_ecoregions.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("EvergreenDeciduousNeedleleafTrees"):
			layers.append(layer)
		elif layer.name().startswith("vegetation_background"):
			layers.append(layer)

	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_pine.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("mixedTrees"):
			layers.append(layer)
		elif layer.name().startswith("vegetation_background"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_mixed.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("DeciduousBroadleafTrees"):
			layers.append(layer)
		elif layer.name().startswith("vegetation_background"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_deciduous.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("EvergreenBroadleafTrees"):
			layers.append(layer)
		elif layer.name().startswith("vegetation_background"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_jungle.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("Shrubs"):
			layers.append(layer)
		elif layer.name().startswith("vegetation_background"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_shrubs.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("HerbaceousVegetation"):
			layers.append(layer)
		elif layer.name().startswith("vegetation_background"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_herbs.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("halfeti_rose"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_wither_rose.png', settings)
	assert ret==0

	####################################################################################


	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("Snow"):
			layers.append(layer)
		elif layer.name().startswith("vegetation_background"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_snow.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("groundcover"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_groundcover.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []

	if borders == 'True':
		for layer in QgsProject.instance().mapLayers().values():
			if layer.name().startswith("cntry" + borderyear):
				layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_border.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("corals"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_corals.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("stream"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_stream.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("ocean_temp"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_ocean_temp.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("longitude"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_longitude.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("latitude"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_latitude.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("aerodrome"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_aerodrome.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("easter_egg"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_easter_eggs.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("gold"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_gold.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("diamond"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_diamond.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("iron"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_iron.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("redstone"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_redstone.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("copper"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_copper.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("netherite"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_netherite.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("coal"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_coal.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("quartz"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()

	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_quartz.png', settings)
	assert ret==0

	####################################################################################

	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)

	layers = []
	for layer in QgsProject.instance().mapLayers().values():
		if layer.name().startswith("clay"):
			layers.append(layer)
		
	for layer in layers:
		node = root.findLayer(layer.id())
		node.setItemVisibilityChecked(Qt.Checked)

	project = QgsProject().instance()
	layout = QgsPrintLayout(project)
	layout.initializeDefaults()

	#select all pages and change the first one to the right dimension
	pages = layout.pageCollection()
	pages.page(0).setPageSize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))

	#create a map inside the layout
	map = QgsLayoutItemMap(layout)
	map.setRect(0, 0, scale, scale)
	map.setExtent(QgsRectangle(xMin, yMin, xMax, yMax))
	map.attemptMove(QgsLayoutPoint(0,0,QgsUnitTypes.LayoutPixels))
	map.attemptResize(QgsLayoutSize(scale,scale,QgsUnitTypes.LayoutPixels))
	layout.addLayoutItem(map)

	#create exporter with settings
	exporter = QgsLayoutExporter(layout)
	settings = QgsLayoutExporter.ImageExportSettings()
	settings.imageSize = (QSize(scale,scale))

	#disable Antialiasing
	context = QgsLayoutRenderContext(layout)
	context.setFlag(context.FlagAntialiasing, False)
	settings.flags = context.flags()
	
	#create image
	ret = exporter.exportToImage(path + 'image_exports/' + tile + '/' + tile + '_clay.png', settings)
	assert ret==0

	####################################################################################

	#at last, disable all layers
	alllayers = []
	for alllayer in QgsProject.instance().mapLayers().values():
		alllayers.append(alllayer)

	root = QgsProject.instance().layerTreeRoot()
	for alllayer in alllayers:
		node = root.findLayer(alllayer.id())
		node.setItemVisibilityChecked(Qt.Unchecked)
