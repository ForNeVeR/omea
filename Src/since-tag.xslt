<?xml version="1.0" encoding="UTF-8" ?>
<!--
SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.

SPDX-License-Identifier: GPL-2.0-only
-->

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

	<xsl:template match="since" mode="after-remarks-section">
		<h4>Appeared in Omea Version or Build: </h4><xsl:value-of select="."/>
	</xsl:template>

</xsl:stylesheet>

