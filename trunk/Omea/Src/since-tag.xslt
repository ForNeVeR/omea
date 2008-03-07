<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	
	<xsl:template match="since" mode="after-remarks-section"> 
		<h4>Appeared in Omea Version or Build: </h4><xsl:value-of select="."/>
	</xsl:template> 
		
</xsl:stylesheet>

  