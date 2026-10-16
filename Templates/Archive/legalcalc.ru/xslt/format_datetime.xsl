<?xml version = "1.0" encoding = "UTF-8"?>
	<xsl:stylesheet version = "1.0" xmlns:xsl = "http://www.w3.org/1999/XSL/Transform">   
	
	<xsl:template name="formatDateTime">
		<xsl:param name="dateTime" />
		<xsl:variable name="date" select="substring-before($dateTime, 'T')" />
		<xsl:variable name="year" select="substring-before($date, '-')" />
		<xsl:variable name="month" select="substring-before(substring-after($date, '-'), '-')" />
		<xsl:variable name="day" select="substring-after(substring-after($date, '-'), '-')" />
		<xsl:choose>
			<xsl:when test="$year=''"></xsl:when>
			<xsl:otherwise><xsl:value-of select="concat($day, '.', $month, '.', $year)"/></xsl:otherwise>			
		</xsl:choose>		
	</xsl:template>
	
	<xsl:template name="formatDate">
		<xsl:param name="dateTime" />		
		<xsl:variable name="year" select="substring-before($dateTime, '-')" />
		<xsl:variable name="month" select="substring-before(substring-after($dateTime, '-'), '-')" />
		<xsl:variable name="day" select="substring-after(substring-after($dateTime, '-'), '-')" />
		<xsl:choose>
			<xsl:when test="$year=''"></xsl:when>
			<xsl:otherwise><xsl:value-of select="concat($day, '.', $month, '.', $year)"/></xsl:otherwise>			
		</xsl:choose>		
	</xsl:template>
	
	<xsl:template name="formatTime">
		<xsl:param name="dateTime" />
		<xsl:value-of select="substring-after($dateTime, 'T')" />
	</xsl:template>	
</xsl:stylesheet>